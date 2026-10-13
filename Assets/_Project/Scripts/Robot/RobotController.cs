using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotController : MonoBehaviour
{
    // Robotun bulunabileceði durumlar (State Machine)
    public enum RobotState { Idle, MoveTo, Scan, Interact }

    [Header("Durum Bilgisi")]
    public RobotState currentState = RobotState.Idle;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        // Ýlk hafta testi: Farenin sol týký ile robota hedef ver (Adým 6.3)
        if (Input.GetMouseButtonDown(0))
        {
            SetTargetFromMouseClick();
        }

        // Durum Makinesi Yönetimi
        switch (currentState)
        {
            case RobotState.Idle:
                // Robot bekliyor
                break;

            case RobotState.MoveTo:
                // Robot hedefe ulaþtý mý kontrol et
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        currentState = RobotState.Idle;
                    }
                }
                break;

            case RobotState.Scan:
                // Ýleride Raycast ile etrafý tarama kodlarý buraya gelecek
                break;

            case RobotState.Interact:
                // Ýleride lazer þalterini indirme animasyonu/kodu buraya gelecek
                break;
        }
    }

    private void SetTargetFromMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            agent.SetDestination(hit.point);
            currentState = RobotState.MoveTo;
        }
    }
}