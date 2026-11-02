using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Breakpoint.Core;
using Breakpoint.Robot;

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
    public bool tersDikeyBakış;
    private float xRotasyonu = 0f;
    private GUIStyle targetHintStyle;

    [Header("El Feneri")]
    public Light elFeneri;

    [Header("Robot Komut Sistemi")]
    [SerializeField] private RobotController robotController;
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
        fareHassasiyeti = PlayerPrefs.GetFloat("Breakpoint.MouseSensitivity", fareHassasiyeti);
        tersDikeyBakış = PlayerPrefs.GetInt("Breakpoint.InvertLookY", 0) == 1;

        // Robot yalnızca koridor sahnesinde bulunur; kapı sahnesinde aramak hata değildir.
        if (SceneManager.GetActiveScene().name != "Level_Corridor")
            return;

        if (robotController == null)
            robotController = FindFirstObjectByType<RobotController>();

        if (robotController == null)
            Debug.LogWarning("PlayerController: RobotController bulunamadı; robot komutları devre dışı.", this);

        // Cursor.lockState ve Cursor.visible satırları SİLİNDİ, imleci artık GameManager yönetiyor
    }

    void Update()
    {
        // Diyalog aktifken oyuncu hareketini ve kamerayı tamamen dondur
        if (isDialogueActive) return;

        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        // 1. KAMERA DÖNDÜRME (Mouse Look)
        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        Vector2 stickLook = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
        float fareX = mouseDelta.x * fareHassasiyeti * 0.1f + stickLook.x * fareHassasiyeti * 100f * Time.deltaTime;
        float fareY = mouseDelta.y * fareHassasiyeti * 0.1f + stickLook.y * fareHassasiyeti * 100f * Time.deltaTime;

        xRotasyonu += tersDikeyBakış ? fareY : -fareY;
        xRotasyonu = Mathf.Clamp(xRotasyonu, -90f, 90f);
        oyuncuKamerasi.localRotation = Quaternion.Euler(xRotasyonu, 0f, 0f);
        transform.Rotate(Vector3.up * fareX);

        // 2. WASD YÜRÜME
        Keyboard keyboard = Keyboard.current;
        Vector2 stickMove = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        float x = (keyboard == null ? 0f :
            (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
            (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f)) + stickMove.x;
        float z = (keyboard == null ? 0f :
            (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
            (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f)) + stickMove.y;
        Vector2 moveInput = Vector2.ClampMagnitude(new Vector2(x, z), 1f);
        
        Vector3 hareket = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(hareket * yürümeHizi * Time.deltaTime);

        // 3. ZIPLAMA VE YERÇEKİMİ
        if (controller.isGrounded && dususHizi.y < 0)
        {
            dususHizi.y = -2f; // Yerdeyken aşağı düşmeyi durdur
        }

        bool jumpPressed = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                           (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (jumpPressed && controller.isGrounded)
        {
            dususHizi.y = Mathf.Sqrt(ziplamaGucu * -2f * yercekimi); // Zıpla
        }

        dususHizi.y += yercekimi * Time.deltaTime;
        controller.Move(dususHizi * Time.deltaTime); // Yerçekimini uygula

        // 4. EL FENERİ (F Tuşu)
        bool flashlightPressed = (keyboard != null && keyboard.fKey.wasPressedThisFrame) ||
                                 (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        if (flashlightPressed && elFeneri != null)
        {
            elFeneri.enabled = !elFeneri.enabled;
        }

        // 5. ROBOTA KOMUT (Sol Tık)
        bool robotCommandPressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                                   (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);
        if (robotCommandPressed)
        {
            RobotaKomutGonder();
        }
    }

    private void RobotaKomutGonder()
    {
        if (robotController == null || oyuncuKamerasi == null)
            return;

        Ray ray = new Ray(oyuncuKamerasi.position, oyuncuKamerasi.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, komutMenzili))
        {
            Debug.Log("Robot komutu hedefi: " + hit.collider.name, hit.collider);
            if (hit.collider.CompareTag("Salter"))
            {
                robotController.MoveToSwitch(hit.collider.transform);
                GameEvents.RaiseNotification("Robot şaltere gönderildi.");
            }
            else
            {
                robotController.MoveTo(hit.point);
                GameEvents.RaiseNotification("Robot seçilen noktaya ilerliyor.");
            }
        }
        else
        {
            Debug.Log("Robot komutu için görüş doğrultusunda hedef bulunamadı.", this);
            GameEvents.RaiseNotification("Hedef görünmüyor. Kamerayı hedefe çevirip tekrar dene.");
        }
    }

    private void OnGUI()
    {
        if (isDialogueActive || (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing))
            return;

        bool aimingAtSwitch = false;
        if (oyuncuKamerasi != null && Physics.Raycast(
                oyuncuKamerasi.position,
                oyuncuKamerasi.forward,
                out RaycastHit hit,
                komutMenzili))
        {
            aimingAtSwitch = hit.collider.CompareTag("Salter");
        }

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        Color reticleColor = aimingAtSwitch ? new Color(0.2f, 1f, 0.45f) : Color.white;
        GUI.color = reticleColor;
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 9f, 2f, 6f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 1f, centerY + 3f, 2f, 6f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 9f, centerY - 1f, 6f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX + 3f, centerY - 1f, 6f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (aimingAtSwitch)
        {
            targetHintStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = reticleColor }
            };
            targetHintStyle.normal.textColor = reticleColor;
            GUI.Label(new Rect(centerX - 170f, centerY + 18f, 340f, 28f), "ŞALTER  ·  SOL TIK / RB: ROBOTU GÖNDER", targetHintStyle);
        }
    }
}
