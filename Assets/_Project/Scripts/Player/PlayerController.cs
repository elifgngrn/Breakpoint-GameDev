using System.Collections;
using System.Linq;
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
    [SerializeField, Min(1f)] private float kosmaCarpani = 1.6f;
    [SerializeField, Min(1f)] private float maksimumDayaniklilik = 5f;
    [SerializeField, Min(0.1f)] private float kosmaEnerjiTuketimi = 1f;
    [SerializeField, Min(0.1f)] private float dinlenmeEnerjiDolumu = 0.8f;
    [SerializeField, Min(0f)] private float enerjiDolumGecikmesi = 0.75f;
    [SerializeField, Min(0.1f)] private float hizlanma = 14f;
    [SerializeField, Min(0.1f)] private float yavaslama = 20f;
    [SerializeField, Min(0.1f)] private float havadaHizlanma = 4f;
    [SerializeField, Min(0f)] private float ziplamaTamponSuresi = 0.15f;
    [SerializeField, Min(0f)] private float kenarZiplamaSuresi = 0.12f;
    [SerializeField] private AudioClip[] adimSesleri;
    [SerializeField, Min(0.05f)] private float yurumeAdimAraligi = 0.5f;
    [SerializeField, Min(0.05f)] private float kosmaAdimAraligi = 0.32f;
    [SerializeField, Range(0f, 1f)] private float adimSesSeviyesi = 0.65f;
    private float currentStamina;
    private float jumpBufferTimer = -1f;
    private float coyoteTimer = -1f;
    private bool wasGrounded;
    private ParticleSystem jumpLandingDust;
    private float adimZamani;
    private AudioSource adimAudioSource;
    private Vector3 yatayHiz;
    private GUIStyle staminaTextStyle;
    private float lastSprintTime;
    private bool sprintLockedOut;
    private bool staminaWarningSent;
    public float ziplamaGucu = 2f;
    public float yercekimi = -15f;
    private CharacterController controller;
    private Vector3 dususHizi;
    public float CurrentPlanarSpeed => new Vector3(yatayHiz.x, 0f, yatayHiz.z).magnitude;
    public bool IsAirborne { get; private set; }
    public float VerticalVelocity => dususHizi.y;

    [Header("Kamera Ayarları")]
    public Transform oyuncuKamerasi;
    public float fareHassasiyeti = 2f;
    public bool tersDikeyBakış;
    [SerializeField, Range(50f, 100f)] private float normalGorusAcisi = 60f;
    [SerializeField, Range(50f, 100f)] private float kosmaGorusAcisi = 68f;
    [SerializeField, Min(0.1f)] private float gorusGecisHizi = 8f;
    private Camera playerCamera;
    private float xRotasyonu = 0f;
    private GUIStyle targetHintStyle;

    [Header("El Feneri")]
    public Light elFeneri;

    [Header("Robot Komut Sistemi")]
    [SerializeField] private RobotController robotController;
    public float komutMenzili = 50f;
    private bool isDialogueActive = false;
    private bool introInteractionLock;

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

    public void PlayWakeUpSequence()
    {
        introInteractionLock = false;
        ProceduralHumanoidAnimator animator = GetComponentInChildren<ProceduralHumanoidAnimator>();
        animator?.PlayWakeUp();
        if (wakeSequence != null) StopCoroutine(wakeSequence);
        wakeSequence = StartCoroutine(WakeUpRoutine());
    }

    private Coroutine wakeSequence;

    private IEnumerator WakeUpRoutine()
    {
        SetDialogueActive(true);
        yield return new WaitForSeconds(1.25f);
        if (this != null) SetDialogueActive(false);
        wakeSequence = null;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        wasGrounded = controller != null && controller.isGrounded;
        jumpLandingDust = CreateJumpLandingDust();
        adimAudioSource = GetComponent<AudioSource>();
        if (adimAudioSource == null && adimSesleri != null && adimSesleri.Length > 0)
            adimAudioSource = gameObject.AddComponent<AudioSource>();
        if (adimAudioSource != null)
            adimAudioSource.playOnAwake = false;
        if (oyuncuKamerasi != null)
        {
            playerCamera = oyuncuKamerasi.GetComponent<Camera>();
            if (playerCamera != null)
                normalGorusAcisi = playerCamera.fieldOfView;
        }
        fareHassasiyeti = PlayerPrefs.GetFloat("Breakpoint.MouseSensitivity", fareHassasiyeti);
        tersDikeyBakış = PlayerPrefs.GetInt("Breakpoint.InvertLookY", 0) == 1;
        currentStamina = maksimumDayaniklilik;
        introInteractionLock = GameManager.Instance != null &&
                               GameManager.Instance.StoryStage == LibraryStoryStage.WakeAtDesk;

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

        // Opening scene: remain seated until E targets the wake-up interaction.
        if (introInteractionLock)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                LibraryStoryInteractable[] storyInteractions =
                    FindObjectsByType<LibraryStoryInteractable>(FindObjectsSortMode.None);
                foreach (LibraryStoryInteractable storyInteraction in storyInteractions)
                {
                    if (storyInteraction.InteractionKind != LibraryInteractionKind.WakeFromDesk)
                        continue;
                    storyInteraction.Interact(this);
                    break;
                }
            }
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            EtkilesimKur();
            if (isDialogueActive) return;
        }

        // 1. KAMERA DÖNDÜRME (Mouse Look)
        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        Vector2 stickLook = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
        float fareX = mouseDelta.x * fareHassasiyeti * 0.1f + stickLook.x * fareHassasiyeti * 100f * Time.deltaTime;
        float fareY = mouseDelta.y * fareHassasiyeti * 0.1f + stickLook.y * fareHassasiyeti * 100f * Time.deltaTime;

        xRotasyonu += tersDikeyBakış ? fareY : -fareY;
        xRotasyonu = Mathf.Clamp(xRotasyonu, -90f, 90f);
        if (oyuncuKamerasi != null)
            oyuncuKamerasi.localRotation = Quaternion.Euler(xRotasyonu, 0f, 0f);
        transform.Rotate(Vector3.up * fareX);

        // 2. WASD YÜRÜME
        Vector2 stickMove = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
        float x = (keyboard == null ? 0f :
            (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
            (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f)) + stickMove.x;
        float z = (keyboard == null ? 0f :
            (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
            (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f)) + stickMove.y;
        Vector2 moveInput = Vector2.ClampMagnitude(new Vector2(x, z), 1f);
        bool sprintRequested = (keyboard != null && keyboard.leftShiftKey.isPressed) ||
                               (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        if (sprintLockedOut && currentStamina >= maksimumDayaniklilik * 0.25f)
        {
            sprintLockedOut = false;
            staminaWarningSent = false;
        }

        bool sprinting = sprintRequested && !sprintLockedOut && currentStamina > 0f && moveInput.sqrMagnitude > 0.01f;
        if (sprinting)
        {
            currentStamina = Mathf.Max(0f, currentStamina - kosmaEnerjiTuketimi * Time.deltaTime);
            lastSprintTime = Time.time;
            if (currentStamina <= 0f)
            {
                sprintLockedOut = true;
                if (!staminaWarningSent)
                {
                    GameEvents.RaiseNotification("Dayanıklılık bitti. Koşmak için biraz dinlen.");
                    staminaWarningSent = true;
                }
            }
        }
        else if (Time.time >= lastSprintTime + enerjiDolumGecikmesi)
            currentStamina = Mathf.Min(maksimumDayaniklilik, currentStamina + dinlenmeEnerjiDolumu * Time.deltaTime);
        float currentMoveSpeed = yürümeHizi * (sprinting ? kosmaCarpani : 1f);

        if (playerCamera != null)
        {
            float targetFov = sprinting ? kosmaGorusAcisi : normalGorusAcisi;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, gorusGecisHizi * Time.deltaTime);
        }
        
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 targetVelocity = moveDirection * currentMoveSpeed;
        float speedChange = moveInput.sqrMagnitude > 0.01f
            ? (controller.isGrounded ? hizlanma : havadaHizlanma)
            : yavaslama;
        yatayHiz = Vector3.MoveTowards(yatayHiz, targetVelocity, speedChange * Time.deltaTime);
        controller.Move(yatayHiz * Time.deltaTime);
        GuncelleAdimSesi(sprinting);

        // 3. ZIPLAMA VE YERÇEKİMİ
        bool grounded = controller.isGrounded;
        if (grounded)
        {
            coyoteTimer = kenarZiplamaSuresi;
            if (dususHizi.y < 0)
                dususHizi.y = -2f; // Yerdeyken aşağı düşmeyi durdur
        }
        else
            coyoteTimer -= Time.deltaTime;

        bool jumpPressed = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
                           (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (jumpPressed)
            jumpBufferTimer = ziplamaTamponSuresi;
        else
            jumpBufferTimer -= Time.deltaTime;

        bool jumpedThisFrame = false;
        if (jumpBufferTimer >= 0f && coyoteTimer >= 0f)
        {
            dususHizi.y = Mathf.Sqrt(ziplamaGucu * -2f * yercekimi); // Zıpla
            jumpBufferTimer = -1f;
            coyoteTimer = -1f;
            jumpedThisFrame = true;
        }

        dususHizi.y += yercekimi * Time.deltaTime;
        controller.Move(dususHizi * Time.deltaTime); // Yerçekimini uygula
        bool groundedAfterMove = controller.isGrounded;
        IsAirborne = !groundedAfterMove;
        if (jumpedThisFrame && jumpLandingDust != null)
        {
            jumpLandingDust.Play();
            jumpLandingDust.Emit(7);
        }
        else if (!wasGrounded && groundedAfterMove && jumpLandingDust != null)
        {
            jumpLandingDust.Play();
            jumpLandingDust.Emit(14);
        }
        wasGrounded = groundedAfterMove;

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

    private void EtkilesimKur()
    {
        // Debris commands are proximity interactions as well as ray interactions.
        // The fallen shelf itself can cover the invisible trigger from the camera ray,
        // so select the closest unfinished command in front of the player first.
        GameManager currentManager = GameManager.Instance;
        if (currentManager != null && currentManager.StoryStage == LibraryStoryStage.ClearDebris)
        {
            LibraryStoryInteractable debrisTarget = FindObjectsByType<LibraryStoryInteractable>(FindObjectsSortMode.None)
                .Where(target => target.IsAvailable)
                .Select(target => new
                {
                    Target = target,
                    Offset = Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up)
                })
                .Where(candidate => candidate.Offset.sqrMagnitude <= 4.8f * 4.8f)
                .OrderBy(candidate => candidate.Offset.sqrMagnitude)
                .Select(candidate => candidate.Target)
                .FirstOrDefault();
            if (debrisTarget != null)
            {
                debrisTarget.Interact(this);
                return;
            }
        }

        // Robot remains usable even when a shelf, chair or its small collider
        // obscures the camera ray during the discovery step.
        GameManager manager = GameManager.Instance;
        if (manager != null && manager.StoryStage == LibraryStoryStage.FindRobot)
        {
            LibraryStoryInteractable robotTarget = FindObjectsByType<LibraryStoryInteractable>(FindObjectsSortMode.None)
                .Where(target => target.InteractionKind == LibraryInteractionKind.ActivateRobot)
                .OrderBy(target => Vector3.SqrMagnitude(target.transform.position - transform.position))
                .FirstOrDefault();
            if (robotTarget != null && Vector3.Distance(robotTarget.transform.position, transform.position) <= 3.2f)
            {
                robotTarget.Interact(this);
                return;
            }
        }
        if (oyuncuKamerasi == null) return;
        Ray ray = new Ray(oyuncuKamerasi.position, oyuncuKamerasi.forward);
        // Third-person camera sits behind the avatar; include that offset in the usable reach.
        RaycastHit[] hits = Physics.RaycastAll(ray, 7.5f);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        RaycastHit hit = default;
        bool foundExternalHit = false;
        foreach (RaycastHit candidate in hits)
        {
            if (candidate.collider.transform == transform || candidate.collider.transform.IsChildOf(transform))
                continue;

            // Story targets can sit behind furniture or visual-only colliders. Give a
            // deliberate story interaction priority across the whole short ray.
            LibraryStoryInteractable storyTarget =
                candidate.collider.GetComponentInParent<LibraryStoryInteractable>();
            if (storyTarget != null)
            {
                storyTarget.Interact(this);
                return;
            }

            if (foundExternalHit)
                continue;
            hit = candidate;
            foundExternalHit = true;
        }
        if (!foundExternalHit) return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact(this);
            return;
        }

        // Existing scenes mark the robot's switch with the Salter tag.
        if (hit.collider.CompareTag("Salter") && robotController != null)
        {
            bool accepted = robotController.MoveToSwitch(hit.collider.transform);
            GameEvents.RaiseNotification(accepted
                ? "Robot şaltere doğru ilerliyor."
                : "Robot şaltere ulaşamıyor; NavMesh bağlantısını kontrol edin.");
        }
    }

    private void GuncelleAdimSesi(bool sprinting)
    {
        if (adimSesleri == null || adimSesleri.Length == 0 || adimAudioSource == null)
            return;

        adimZamani -= Time.deltaTime;
        bool walkingOnGround = controller.isGrounded && yatayHiz.sqrMagnitude > 0.1f;
        if (!walkingOnGround)
        {
            adimZamani = 0f;
            return;
        }

        if (adimZamani > 0f)
            return;

        AudioClip clip = adimSesleri[Random.Range(0, adimSesleri.Length)];
        if (clip != null)
            adimAudioSource.PlayOneShot(clip, adimSesSeviyesi);

        adimZamani = sprinting ? kosmaAdimAraligi : yurumeAdimAraligi;
    }

    private ParticleSystem CreateJumpLandingDust()
    {
        GameObject effectObject = new GameObject("Jump_Landing_Dust");
        effectObject.transform.SetParent(transform, false);
        effectObject.transform.localPosition = new Vector3(0f, .06f, 0f);
        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = .35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.22f, .42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.55f, 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(.07f, .14f);
        main.startColor = new Color(.72f, .62f, .48f, .72f);
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = .2f;
        return particles;
    }

    private void RobotaKomutGonder()
    {
        if (robotController == null || !TryGetExternalCameraHit(komutMenzili, out RaycastHit hit))
            return;

        Debug.Log("Robot komutu hedefi: " + hit.collider.name, hit.collider);
        if (hit.collider.CompareTag("Salter"))
        {
            bool accepted = robotController.MoveToSwitch(hit.collider.transform);
            GameEvents.RaiseNotification(accepted
                ? "Robot şaltere gönderildi."
                : "Robot şaltere ulaşamıyor; NavMesh bağlantısını kontrol et.");
        }
        else
        {
            bool accepted = robotController.MoveTo(hit.point);
            GameEvents.RaiseNotification(accepted
                ? "Robot seçilen noktaya ilerliyor."
                : "Bu hedefin yakınında yürünebilir zemin yok. Zemine nişan alıp tekrar dene.");
        }
    }

    private bool TryGetExternalCameraHit(float distance, out RaycastHit hit)
    {
        hit = default;
        if (oyuncuKamerasi == null) return false;

        RaycastHit[] hits = Physics.RaycastAll(
            oyuncuKamerasi.position,
            oyuncuKamerasi.forward,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (RaycastHit candidate in hits)
        {
            Transform target = candidate.collider.transform;
            if (target == transform || target.IsChildOf(transform)) continue;
            hit = candidate;
            return true;
        }
        return false;
    }

    private void OnGUI()
    {
        if (isDialogueActive || (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing))
            return;

        bool aimingAtSwitch = TryGetExternalCameraHit(komutMenzili, out RaycastHit hit) && hit.collider.CompareTag("Salter");

        DrawStaminaBar();

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

    private void DrawStaminaBar()
    {
        bool sprintHeld = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
                          (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        if (currentStamina >= maksimumDayaniklilik && !sprintHeld)
            return;

        const float width = 180f;
        const float height = 8f;
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height * 0.72f;
        GUI.color = new Color(0.04f, 0.07f, 0.08f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = currentStamina <= 1f ? new Color(1f, 0.35f, 0.2f) : new Color(0.25f, 0.85f, 0.7f);
        GUI.DrawTexture(new Rect(x, y, width * Mathf.Clamp01(currentStamina / maksimumDayaniklilik), height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        staminaTextStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            normal = { textColor = new Color(0.82f, 0.9f, 0.88f) }
        };
        GUI.Label(new Rect(x, y + height + 3f, width, 18f),
            $"ENERJİ  {currentStamina:0.0} / {maksimumDayaniklilik:0.0}", staminaTextStyle);
    }
}
