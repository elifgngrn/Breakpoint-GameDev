using UnityEngine;
using UnityEngine.AI;
using Core;
using Breakpoint.Core;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotController : MonoBehaviour
{
    [Header("Etkileşim Ayarları")]
    public GameObject lazerBariyeri;
    public enum RobotState { Idle, MoveTo, Scan, Interact }
    
    [Header("Durum Bilgisi")]
    public RobotState currentState = RobotState.Idle;

    [Header("Sensör Ayarları")]
    public float sensorRange = 5.0f; // Raycast'in menzili
    public Transform sensorPoint; // Işının çıkacağı nokta (Robotun göz hizası)

    private NavMeshAgent agent;

    void Start()
{
    agent = GetComponent<NavMeshAgent>();
    Debug.Log("ROBOT SCRİPTİ BAŞARIYLA BAŞLADI!"); // Sistemi zorla konuşturuyoruz
}

    void Update()
    {
        switch (currentState)
        {
            case RobotState.Idle:
                break;

            case RobotState.MoveTo:
                // Eğer yol hesaplaması bittiyse ve hedefe yarım metre (0.5f) yaklaştıysak
                if (!agent.pathPending && agent.remainingDistance <= 0.5f)
                {
                    agent.isStopped = true; // Robotun fiziksel titremesini/kaymasını zorla durdur
                    currentState = RobotState.Scan; // Taramaya geç
                }
                break;
                
            case RobotState.Scan:
                PerformRaycastScan();
                break;
                
            case RobotState.Interact:
                // Şalteri gördüğünde bu kod çalışacak
                if (lazerBariyeri != null) 
                {
                    lazerBariyeri.SetActive(false); // SetActive(false) komutu objeyi sahnede görünmez ve etkisiz yapar
                    GameEvents.RaiseLasersDisabled();
                    Debug.Log("Lazerler kapatıldı, yol açıldı!");
                    
                    // Görev bittikten sonra robotu tekrar bekleme moduna alalım ki sürekli kodu çalıştırmasın
                    currentState = RobotState.Idle; 
                }
                break;
        }
    }

    private void PerformRaycastScan()
    {
        RaycastHit hit;
        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward);
        Vector3 origin = sensorPoint != null ? sensorPoint.position : transform.position;

        // Çizgiyi 5 saniye boyunca ekranda tut (görebilmen için)
        Debug.DrawRay(origin, forwardDirection * sensorRange, Color.red, 5f);

        if (Physics.Raycast(origin, forwardDirection, out hit, sensorRange))
        {
            // Işın neye çarparsa çarpsın ismini konsola yazdır
            Debug.Log("Işın şuna çarptı: " + hit.collider.name); 

            if (hit.collider.CompareTag("Salter"))
            {
                Debug.Log("Şalter Başarıyla Algılandı!");
                currentState = RobotState.Interact;
            }
        }
        else
        {
            Debug.Log("Işın boşluğa gidiyor, hiçbir şeye çarpmadı.");
        }
    }
}