using System.Collections;
using UnityEngine;
using UnityEngine.AI;
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
            if (behaviorCoroutine == null) return;

            StopCoroutine(behaviorCoroutine);
            behaviorCoroutine = null;
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
        public void MoveTo(Vector3 destination)
        {
            commandedSwitch = null;
            SetDestination(destination);
        }

        /// <summary>Oyuncunun tıkladığı şaltere robotu gönderir ve yaklaşınca etkileştirir.</summary>
        public void MoveToSwitch(Transform switchTransform)
        {
            if (switchTransform == null || !switchTransform.CompareTag("Salter"))
                return;

            commandedSwitch = switchTransform;
            if (!SetDestination(switchTransform.position))
                commandedSwitch = null;
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

            if (!NavMesh.SamplePosition(destination, out NavMeshHit navHit, 2f, agent.areaMask))
            {
                Debug.LogWarning("RobotController: Hedefin yakınında yürünebilir NavMesh bulunamadı.", this);
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
            if (agent != null)
                agent.isStopped = false;
        }

        private void UpdateFollowState()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= agent.stoppingDistance)
            {
                currentState = RobotState.Idle;
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;

            if (!agent.pathPending)
                agent.SetDestination(playerTransform.position);

            if (!agent.pathPending && distanceToPlayer > agent.stoppingDistance && agent.velocity.sqrMagnitude < 0.1f)
                ReportFrontObstacle();
        }

        private void UpdateMoveToTargetState()
        {
            if (commandedSwitch != null)
            {
                if (!commandedSwitch.gameObject.activeInHierarchy)
                {
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
            }
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
            commandedSwitch = null;

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

            if (barrierWasActive && switchTransform != null)
                PlaySwitchFeedback(switchTransform);

            FollowPlayer();
        }

        private void PlaySwitchFeedback(Transform switchTransform)
        {
            if (switchChime != null && feedbackAudio != null)
                feedbackAudio.PlayOneShot(switchChime, 0.65f);

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
