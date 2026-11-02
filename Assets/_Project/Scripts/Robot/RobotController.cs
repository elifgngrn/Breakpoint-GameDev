using UnityEngine;
using UnityEngine.AI;
using Core;
using Breakpoint.Core;

namespace Breakpoint.Robot
{
    /// <summary>
    /// Companion robot davranışlarını NavMeshAgent ve küçük bir FSM üzerinden yönetir.
    /// Takip hedefi ve hareket aralığı Inspector'dan yapılandırılabilir.
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

        [Header("Takip")]
        [SerializeField] private Transform playerTransform;
        [SerializeField, Min(0f)] private float followStoppingDistance = 2.0f;
        [SerializeField, Min(0.02f)] private float updateInterval = 0.2f;

        [Header("Şalter etkileşimi")]
        [SerializeField] private GameObject lazerBariyeri;
        [SerializeField, Min(0f)] private float sensorRange = 5.0f;
        [SerializeField] private Transform sensorPoint;

        [Header("Durum")]
        [SerializeField] private RobotState currentState = RobotState.Idle;

        private NavMeshAgent agent;
        private float nextDestinationUpdateTime;
        private bool dependenciesValid;

        public RobotState CurrentState => currentState;

        private void Awake()
        {
            // Sahne referansı Inspector'dan verilmemişse Player etiketiyle otomatik bul.
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    playerTransform = playerObj.transform;
            }

            // RequireComponent normalde ajanı sağlar; kontrol, eksik/bozuk sahne durumlarını da kapsar.
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogWarning("RobotController: NavMeshAgent bulunamadı; robot davranışı durduruldu.", this);
                enabled = false;
                return;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("RobotController: Player Transform atanmamış; robot davranışı durduruldu. Inspector'dan oyuncuyu bağlayın.", this);
                enabled = false;
                return;
            }

            agent.stoppingDistance = followStoppingDistance;
            dependenciesValid = true;
        }

        private void Update()
        {
            if (!dependenciesValid || agent == null || playerTransform == null)
                return;

            switch (currentState)
            {
                case RobotState.Idle:
                case RobotState.FollowPlayer:
                    UpdateFollowState();
                    break;

                case RobotState.MoveToTarget:
                    // Mevcut FSM'de başka bir sistem hedef atadığında robot hedefe NavMesh ile gider.
                    if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        agent.isStopped = true;
                        currentState = RobotState.ScanInteract;
                    }
                    break;

                case RobotState.ScanInteract:
                    ScanForSwitch();
                    break;
            }
        }

        /// <summary>Robotu belirli bir dünya hedefine yöneltir; hareket NavMeshAgent tarafından yürütülür.</summary>
        public void MoveTo(Vector3 destination)
        {
            if (!dependenciesValid || agent == null)
                return;

            agent.isStopped = false;
            if (agent.SetDestination(destination))
                currentState = RobotState.MoveToTarget;
        }

        /// <summary>Özel hedef hareketini bitirip oyuncu takibini yeniden etkinleştirir.</summary>
        public void FollowPlayer()
        {
            if (!dependenciesValid)
                return;

            currentState = RobotState.FollowPlayer;
            nextDestinationUpdateTime = 0f;
        }

        private void UpdateFollowState()
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= followStoppingDistance)
            {
                currentState = RobotState.Idle;
                agent.isStopped = true;
                return;
            }

            currentState = RobotState.FollowPlayer;
            agent.isStopped = false;

            // Hedefi aralıklı güncellemek gereksiz NavMesh hesaplamalarını azaltır.
            if (Time.time < nextDestinationUpdateTime)
                return;

            agent.SetDestination(playerTransform.position);
            nextDestinationUpdateTime = Time.time + Mathf.Max(0.02f, updateInterval);
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

            if (lazerBariyeri != null)
            {
                lazerBariyeri.SetActive(false);
                GameEvents.RaiseLasersDisabled();
                Debug.Log("Robot şalteri algıladı; lazer bariyeri devre dışı bırakıldı.", this);
            }
            else
            {
                Debug.LogWarning("RobotController: Lazer bariyeri referansı atanmamış.", this);
            }

            FollowPlayer();
        }

        private void OnValidate()
        {
            followStoppingDistance = Mathf.Max(0f, followStoppingDistance);
            updateInterval = Mathf.Max(0.02f, updateInterval);
            sensorRange = Mathf.Max(0f, sensorRange);
        }
    }
}
