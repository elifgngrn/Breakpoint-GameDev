using UnityEngine;
using UnityEngine.AI;
using Breakpoint.Core;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float yürümeHizi = 5f;
    public float ziplamaGucu = 2f;
    public float yercekimi = -15f;
    private CharacterController controller;
    private Vector3 dususHizi;

    [Header("Kamera Ayarları")]
    public Transform oyuncuKamerasi;
    public float fareHassasiyeti = 2f;
    private float xRotasyonu = 0f;

    [Header("El Feneri")]
    public Light elFeneri;

    [Header("Robot Komut Sistemi")]
    public NavMeshAgent robot;
    public float komutMenzili = 50f;
    private bool isDialogueActive = false;

    // Bu fonksiyonu PlayerController sınıfının içine ekle:
    public void SetDialogueActive(bool active)
    {
        isDialogueActive = active;
        if (active)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Cursor.lockState ve Cursor.visible satırları SİLİNDİ, imleci artık GameManager yönetiyor
    }

    void Update()
    {
        // Diyalog aktifken oyuncu hareketini ve kamerayı tamamen dondur
        if (isDialogueActive) return;

        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        // 1. KAMERA DÖNDÜRME (Mouse Look)
        float fareX = Input.GetAxis("Mouse X") * fareHassasiyeti;
        float fareY = Input.GetAxis("Mouse Y") * fareHassasiyeti;

        xRotasyonu -= fareY;
        xRotasyonu = Mathf.Clamp(xRotasyonu, -90f, 90f);
        oyuncuKamerasi.localRotation = Quaternion.Euler(xRotasyonu, 0f, 0f);
        transform.Rotate(Vector3.up * fareX);

        // 2. WASD YÜRÜME
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 hareket = transform.right * x + transform.forward * z;
        controller.Move(hareket * yürümeHizi * Time.deltaTime);

        // 3. ZIPLAMA VE YERÇEKİMİ
        if (controller.isGrounded && dususHizi.y < 0)
        {
            dususHizi.y = -2f; // Yerdeyken aşağı düşmeyi durdur
        }

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            dususHizi.y = Mathf.Sqrt(ziplamaGucu * -2f * yercekimi); // Zıpla
        }

        dususHizi.y += yercekimi * Time.deltaTime;
        controller.Move(dususHizi * Time.deltaTime); // Yerçekimini uygula

        // 4. EL FENERİ (F Tuşu)
        if (Input.GetKeyDown(KeyCode.F) && elFeneri != null)
        {
            elFeneri.enabled = !elFeneri.enabled;
        }

        // 5. ROBOTA KOMUT (Sol Tık)
        if (Input.GetMouseButtonDown(0))
        {
            RobotaKomutGonder();
        }
    }

    private void RobotaKomutGonder()
    {
        Ray ray = new Ray(oyuncuKamerasi.position, oyuncuKamerasi.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, komutMenzili))
        {
            if (robot != null) robot.SetDestination(hit.point);
        }
    }
}