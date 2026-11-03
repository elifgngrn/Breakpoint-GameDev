using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Breakpoint.Core;

namespace Breakpoint.Robot
{
    /// <summary>
    /// Companion robot davranışlarını NavMeshAgent ve küçük bir FSM üzerinden yönetir.
    /// Takip hedefi ve şalter etkileşimi aralıklı bir coroutine ile güncellenir.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class RobotController : MonoBehaviour
    {
        public enum RobotState
        {
            Idle,
            FollowPlayer,
            MoveToTarget,
            ScanInteract
        }

        private const float FollowStoppingDistance = 1.8f;
        private const float FollowUpdateInterval = 0.15f;
        private const float StuckWarningInterval = 1f;

        [Header("Takip")]
        [SerializeField] private Transform playerTransform;

        [Header("Şalter etkileşimi")]
        [SerializeField] private GameObject lazerBariyeri;
        [SerializeField, Min(0f)] private float sensorRange = 5f;
        [SerializeField] private Transform sensorPoint;
        [SerializeField, Min(0.1f)] private float stuckRayDistance = 1.2f;

        [Header("Durum")]
        [SerializeField] private RobotState currentState = RobotState.Idle;

        private NavMeshAgent agent;
        private Coroutine behaviorCoroutine;
        private Transform commandedSwitch;
        private float nextStuckWarningTime;
        private bool dependenciesValid;
        private AudioSource feedbackAudio;
        private AudioClip switchChime;
        private Vector3 lastFollowDestination;
        private bool hasFollowDestination;
        private bool recoveryActive;
        private Vector3 recoveryDestination;
        private float recoveryDeadline;
        private bool switchTrialActive;
        private Vector3 switchTrialStartPosition;
        private float switchTrialStartedAt;

        public RobotState CurrentState => currentState;

        private void Awake()
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    playerTransform = playerObj.transform;
            }

            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogWarning("RobotController: NavMeshAgent bulunamadı; robot davranışı durduruldu.", this);
                enabled = false;
                return;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("RobotController: Player Transform bulunamadı; Inspector'dan oyuncuyu bağlayın.", this);
                enabled = false;
                return;
            }

            agent.stoppingDistance = FollowStoppingDistance;
            agent.autoBraking = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            feedbackAudio = GetComponent<AudioSource>();
            if (feedbackAudio == null)
                feedbackAudio = gameObject.AddComponent<AudioSource>();
            feedbackAudio.playOnAwake = false;
            feedbackAudio.spatialBlend = 0f;
            switchChime = CreateSwitchChime();
            dependenciesValid = true;
        }

        private void OnEnable()
        {
            if (dependenciesValid && behaviorCoroutine == null)
                behaviorCoroutine = StartCoroutine(BehaviorLoop());
        }

        private void OnDisable()
        {
            if (behaviorCoroutine != null)
            {
                StopCoroutine(behaviorCoroutine);
                behaviorCoroutine = null;
            }

            if (Gamepad.current != null)
                Gamepad.current.SetMotorSpeeds(0f, 0f);
        }

        private IEnumerator BehaviorLoop()
        {
            var wait = new WaitForSeconds(FollowUpdateInterval);

            while (true)
            {
                if (dependenciesValid && agent != null && playerTransform != null)
                {
                    switch (currentState)
                    {
                        case RobotState.Idle:
                        case RobotState.FollowPlayer:
                            UpdateFollowState();
                            break;

                        case RobotState.MoveToTarget:
                            UpdateMoveToTargetState();
                            break;

                        case RobotState.ScanInteract:
                            ScanForSwitch();
                            break;
                    }
                }

                yield return wait;
            }
        }

        /// <summary>Robotu belirli bir dünya hedefine yöneltir.</summary>
        public bool MoveTo(Vector3 destination)
        {
            commandedSwitch = null;
            return SetDestination(destination);
        }

        /// <summary>Oyuncunun tıkladığı şaltere robotu gönderir ve yaklaşınca etkileştirir.</summary>
        public bool MoveToSwitch(Transform switchTransform)
        {
            if (switchTransform == null || !switchTransform.CompareTag("Salter"))
                return false;

            if (switchTrialActive)
                CompleteSwitchTrial("CANCELLED_BY_NEW_COMMAND", commandedSwitch);

            commandedSwitch = switchTransform;
            Vector3 trialStartPosition = transform.position;
            if (!SetDestination(switchTransform.position))
            {
                string rejectedStart = trialStartPosition.ToString("F2");
                string rejectedTarget = switchTransform.position.ToString("F2");
                Debug.LogWarning(
                    $"NAVMESH_SWITCH_TRIAL | result=COMMAND_REJECTED | start={rejectedStart} | target={rejectedTarget} | seconds=0.00",
                    this);
                commandedSwitch = null;
                return false;
            }

            switchTrialActive = true;
            switchTrialStartPosition = trialStartPosition;
            switchTrialStartedAt = Time.time;
            string acceptedStart = switchTrialStartPosition.ToString("F2");
            string acceptedTarget = switchTransform.position.ToString("F2");
            Debug.Log(
                $"NAVMESH_SWITCH_TRIAL | result=STARTED | start={acceptedStart} | target={acceptedTarget}",
                this);
            return true;
        }

        private bool SetDestination(Vector3 destination)
        {
            if (!dependenciesValid || agent == null)
                return false;

            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("RobotController: Robot NavMesh üzerinde değil; hedef alınamadı.", this);
                return false;
            }

            // Raycast hedefi duvar yüzeyinde/yüksekte kalabilir. Y konumunu robotun NavMesh
            // yüksekliğine indir ve en yakın yürünebilir noktayı kademeli olarak ara.
            destination.y = agent.nextPosition.y;
            NavMeshHit navHit = default;
            bool foundNavMesh = false;
            float[] sampleRadii = { 2f, 4f, 8f, 12f };
            foreach (float sampleRadius in sampleRadii)
            {
                if (!NavMesh.SamplePosition(destination, out navHit, sampleRadius, agent.areaMask))
                    continue;

                foundNavMesh = true;
                break;
            }

            if (!foundNavMesh)
            {
                Debug.LogWarning("RobotController: Hedefin 12 birim yakınında yürünebilir NavMesh bulunamadı. Hedef: " + destination, this);
                return false;
            }
            destination = navHit.position;

            agent.isStopped = false;
            if (agent.SetDestination(destination))
            {
                currentState = RobotState.MoveToTarget;
                return true;
            }

            Debug.LogWarning("RobotController: Hedef için NavMesh yolu oluşturulamadı.", this);
            return false;
        }

        /// <summary>Özel hedef hareketini bitirip oyuncu takibini yeniden etkinleştirir.</summary>
        public void FollowPlayer()
        {
            if (!dependenciesValid)
                return;

            currentState = RobotState.FollowPlayer;
            hasFollowDestination = false;
            if (agent != null)
                agent.isStopped = false;
        }

        private void UpdateFollowState()
        {
            if (recoveryActive)
            {
                if (agent.pathPending)
                    return;

                if (Vector3.Distance(transform.position, recoveryDestination) <= 0.6f || Time.time >= recoveryDeadline)
                {
                    recoveryActive = false;
                    hasFollowDestination = false;
                    GameEvents.RaiseNotification("Robot yeniden oyuncuyu takip ediyor.");
                }
                else
                {
                    return;
                }
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= agent.stoppingDistance)
            {
                currentState = RobotState.Idle;
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;

            bool playerMovedEnough = !hasFollowDestination ||
                                     (playerTransform.position - lastFollowDestination).sqrMagnitude >= 0.25f;
            bool pathNeedsRepair = !agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid;
            if (!agent.pathPending && (playerMovedEnough || pathNeedsRepair))
            {
                if (agent.SetDestination(playerTransform.position))
                {
                    lastFollowDestination = playerTransform.position;
                    hasFollowDestination = true;
                }
            }

            if (!agent.pathPending && distanceToPlayer > agent.stoppingDistance && agent.velocity.sqrMagnitude < 0.1f)
                ReportFrontObstacle();
        }

        private void UpdateMoveToTargetState()
        {
            if (commandedSwitch != null)
            {
                if (!commandedSwitch.gameObject.activeInHierarchy)
                {
                    CompleteSwitchTrial("TARGET_DISABLED", commandedSwitch);
                    commandedSwitch = null;
                    FollowPlayer();
                    return;
                }

                float switchDistance = Vector3.Distance(transform.position, commandedSwitch.position);
                if (switchDistance <= Mathf.Max(agent.stoppingDistance + 0.75f, 2f))
                {
                    ActivateSwitch(commandedSwitch);
                    return;
                }
            }

            if (agent.pathPending)
                return;

            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                // Kısmi yolun sonuna varmadan hedefi iptal etme; aksi halde robot ilk
                // NavMesh kopukluğunda hemen oyuncu takibine döner ve ilerlemiyor görünür.
                if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.1f)
                    return;

                if (commandedSwitch != null)
                {
                    // Switch command is explicit: operate it from the closest reachable point.
                    Debug.LogWarning("RobotController: Şaltere tam yol yok; robot erişebildiği en yakın noktadan şalteri çalıştırıyor.", this);
                    GameEvents.RaiseNotification("Robot şalteri erişebildiği en yakın noktadan çalıştırdı.");
                    ActivateSwitch(commandedSwitch);
                    return;
                }

                // Serbest hedefte de erişilebilir son noktaya kadar yürüdükten sonra dur.
                Debug.LogWarning("RobotController: Hedefe tam NavMesh yolu yok; erişilebilir en yakın noktada duruldu.", this);
                agent.isStopped = true;
                currentState = RobotState.Idle;
                return;
            }

            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("RobotController: Hedef için geçerli NavMesh yolu oluşturulamadı.", this);
                CompleteSwitchTrial("PATH_INVALID", commandedSwitch);
                commandedSwitch = null;
                FollowPlayer();
                return;
            }

            float distanceToDestination = Vector3.Distance(transform.position, agent.destination);
            if (!agent.hasPath)
            {
                if (distanceToDestination <= agent.stoppingDistance)
                {
                    agent.isStopped = true;
                    currentState = RobotState.ScanInteract;
                }
                return;
            }

            if (distanceToDestination <= agent.stoppingDistance && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.isStopped = true;
                currentState = RobotState.ScanInteract;
            }
        }

        private void ReportFrontObstacle()
        {
            if (Time.time < nextStuckWarningTime)
                return;

            nextStuckWarningTime = Time.time + StuckWarningInterval;
            Vector3 origin = sensorPoint != null
                ? sensorPoint.position
                : transform.position + Vector3.up * 0.5f;
            Vector3 direction = transform.forward;
            Debug.DrawRay(origin, direction * stuckRayDistance, Color.yellow, FollowUpdateInterval);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, stuckRayDistance))
            {
                if (hit.collider.transform == playerTransform || hit.collider.transform.IsChildOf(playerTransform))
                    return;

                Debug.LogWarning("Robot takıldı! Engel: " + hit.collider.name, this);
                TryRecoverAroundObstacle(hit.normal);
            }
        }

        private void TryRecoverAroundObstacle(Vector3 obstacleNormal)
        {
            if (Time.time < recoveryDeadline || !agent.isOnNavMesh)
                return;

            Vector3 towardPlayer = playerTransform.position - transform.position;
            towardPlayer.y = 0f;
            if (towardPlayer.sqrMagnitude < 0.01f)
                return;
            towardPlayer.Normalize();

            Vector3 side = Vector3.Cross(Vector3.up, obstacleNormal).normalized;
            if (side.sqrMagnitude < 0.01f)
                side = Vector3.Cross(Vector3.up, towardPlayer).normalized;

            NavMeshPath bestPath = null;
            Vector3 bestDestination = Vector3.zero;
            float bestPathLength = float.PositiveInfinity;

            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector3 probe = transform.position + (towardPlayer + side * direction).normalized * 2.5f;
                if (!NavMesh.SamplePosition(probe, out NavMeshHit navHit, 1.5f, agent.areaMask))
                    continue;

                NavMeshPath candidatePath = new NavMeshPath();
                if (!NavMesh.CalculatePath(agent.nextPosition, navHit.position, agent.areaMask, candidatePath) ||
                    candidatePath.status != NavMeshPathStatus.PathComplete)
                    continue;

                float pathLength = 0f;
                Vector3[] corners = candidatePath.corners;
                for (int i = 1; i < corners.Length; i++)
                    pathLength += Vector3.Distance(corners[i - 1], corners[i]);

                if (pathLength < bestPathLength)
                {
                    bestPathLength = pathLength;
                    bestPath = candidatePath;
                    bestDestination = navHit.position;
                }
            }

            if (bestPath == null || !agent.SetDestination(bestDestination))
                return;

            recoveryDestination = bestDestination;
            recoveryDeadline = Time.time + 3.5f;
            recoveryActive = true;
            hasFollowDestination = true;
            lastFollowDestination = playerTransform.position;
            Debug.Log("Robot engelin yanından alternatif rota deniyor.", this);
            GameEvents.RaiseNotification("Robot engeli dolaşmak için yan rota deniyor.");
        }

        private void ScanForSwitch()
        {
            Vector3 origin = sensorPoint != null ? sensorPoint.position : transform.position;
            Vector3 direction = transform.forward;
            Debug.DrawRay(origin, direction * sensorRange, Color.red);

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, sensorRange))
                return;

            if (!hit.collider.CompareTag("Salter"))
                return;

            ActivateSwitch(hit.collider.transform);
        }

        private void ActivateSwitch(Transform switchTransform)
        {
            bool barrierWasActive = lazerBariyeri != null && lazerBariyeri.activeSelf;

            if (lazerBariyeri != null)
            {
                if (lazerBariyeri.activeSelf)
                {
                    lazerBariyeri.SetActive(false);
                    GameEvents.RaiseLasersDisabled();
                    GameEvents.RaiseNotification("Şalter çalıştı. Lazer bariyeri kapandı!");
                    Debug.Log("Robot şalteri algıladı; lazer bariyeri devre dışı bırakıldı.", this);
                }
            }
            else
            {
                Debug.LogWarning("RobotController: Lazer bariyeri referansı atanmamış.", this);
            }

            if (switchTransform != null)
                Debug.Log("Robot şalterle etkileşime geçti: " + switchTransform.name, this);

            string trialResult = lazerBariyeri == null
                ? "SWITCH_REACHED_NO_BARRIER_REFERENCE"
                : barrierWasActive ? "SUCCESS" : "SWITCH_REACHED_BARRIER_ALREADY_OFF";
            CompleteSwitchTrial(trialResult, switchTransform);
            commandedSwitch = null;

            if (barrierWasActive && switchTransform != null)
                PlaySwitchFeedback(switchTransform);

            FollowPlayer();
        }

        private void CompleteSwitchTrial(string result, Transform switchTransform)
        {
            if (!switchTrialActive)
                return;

            string targetPosition = switchTransform != null ? switchTransform.position.ToString("F2") : "unavailable";
            string startPosition = switchTrialStartPosition.ToString("F2");
            string endPosition = transform.position.ToString("F2");
            float elapsedSeconds = Time.time - switchTrialStartedAt;
            Debug.Log(
                $"NAVMESH_SWITCH_TRIAL | result={result} | start={startPosition} | end={endPosition} | target={targetPosition} | path={agent.pathStatus} | seconds={elapsedSeconds:F2}",
                this);
            switchTrialActive = false;
        }

        private void PlaySwitchFeedback(Transform switchTransform)
        {
            if (switchChime != null && feedbackAudio != null)
                feedbackAudio.PlayOneShot(switchChime, 0.65f);

            if (Gamepad.current != null && PlayerPrefs.GetInt("Breakpoint.GamepadRumble", 1) == 1)
            {
                Gamepad gamepad = Gamepad.current;
                gamepad.SetMotorSpeeds(0.25f, 0.55f);
                StartCoroutine(StopGamepadRumble(gamepad, 0.28f));
            }

            Renderer[] renderers = switchTransform.GetComponentsInChildren<Renderer>();
            foreach (Renderer switchRenderer in renderers)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                switchRenderer.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", new Color(0.1f, 1f, 0.35f) * 3f);
                switchRenderer.SetPropertyBlock(block);
            }

            GameObject pulseObject = new GameObject("RobotSwitchPulse");
            pulseObject.transform.position = switchTransform.position + Vector3.up * 0.7f;
            Light pulseLight = pulseObject.AddComponent<Light>();
            pulseLight.type = LightType.Point;
            pulseLight.color = new Color(0.15f, 1f, 0.4f);
            pulseLight.range = 4f;
            pulseLight.intensity = 3f;
            Destroy(pulseObject, 0.45f);
            StartCoroutine(ClearSwitchFlash(renderers, 0.45f));
        }

        private static IEnumerator StopGamepadRumble(Gamepad gamepad, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (gamepad != null && gamepad.added)
                gamepad.SetMotorSpeeds(0f, 0f);
        }

        private IEnumerator ClearSwitchFlash(Renderer[] renderers, float duration)
        {
            yield return new WaitForSeconds(duration);
            foreach (Renderer switchRenderer in renderers)
            {
                if (switchRenderer != null)
                    switchRenderer.SetPropertyBlock(null);
            }
        }

        private static AudioClip CreateSwitchChime()
        {
            const int sampleRate = 22050;
            const float duration = 0.32f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / sampleCount);
                float frequency = Mathf.Lerp(660f, 990f, t / duration);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.22f;
            }

            AudioClip clip = AudioClip.Create("RobotSwitchChime", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnValidate()
        {
            sensorRange = Mathf.Max(0f, sensorRange);
            stuckRayDistance = Mathf.Max(0.1f, stuckRayDistance);
        }
    }
}
