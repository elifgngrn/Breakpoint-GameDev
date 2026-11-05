using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using Breakpoint.Core;
using Breakpoint.Robot;

public sealed class ClockHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Header("Kapı Sahnesi Ortam Sesleri")]
    [SerializeField] private AudioClip roomToneClip;
    [SerializeField] private AudioClip clockTickClip;
    [SerializeField] private Transform clockAudioPoint;
    [SerializeField, Range(0f, 1f)] private float roomToneVolume = 0.14f;
    [SerializeField, Range(0f, 1f)] private float clockTickVolume = 0.24f;
    private TMP_Text objectiveLabel;
    private GameObject objectivePanel;
    private GameObject robotStatusPanel;
    private TMP_Text robotStatusLabel;
    private Image robotStatusDot;
    private RobotController robotController;
    private AudioSource roomToneSource;
    private AudioSource clockTickSource;
    private GameObject clockAudioObject;
    private bool doorAudioPaused;
    private TMP_Text notificationLabel;
    private CanvasGroup notificationGroup;
    private Coroutine notificationRoutine;

    private void OnEnable() => GameEvents.NotificationRequested += ShowNotification;

    private void OnDisable() => GameEvents.NotificationRequested -= ShowNotification;

    private void Start()
    {
        if (label == null || label.transform.parent == null) return;

        ConfigureClockLabel();
        CreateObjectivePanel();
        CreateRobotStatusPanel();
        CreateNotification();
        CreateDoorSceneAudio();
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (label == null || gm == null || gm.Clock == null) return;

        bool showHud = gm.State != GameState.Won && gm.State != GameState.Lost;
        UpdateDoorAudioPause(gm.State == GameState.Paused || gm.State == GameState.Won || gm.State == GameState.Lost);
        if (label.gameObject.activeSelf != showHud)
            label.gameObject.SetActive(showHud);
        if (objectivePanel != null && objectivePanel.activeSelf != showHud)
            objectivePanel.SetActive(showHud);
        UpdateRobotStatus(showHud);
        if (!showHud) return;

        label.text = gm.Clock.Formatted;
        if (objectiveLabel != null)
            objectiveLabel.text = GetObjectiveText(gm);
    }

    private void CreateDoorSceneAudio()
    {
        if (roomToneClip != null)
        {
            roomToneSource = gameObject.AddComponent<AudioSource>();
            roomToneSource.clip = roomToneClip;
            roomToneSource.loop = true;
            roomToneSource.playOnAwake = false;
            roomToneSource.spatialBlend = 0f;
            roomToneSource.volume = roomToneVolume;
            roomToneSource.Play();
        }

        if (clockTickClip != null)
        {
            clockAudioObject = new GameObject("DeskClockAudio");
            clockAudioObject.transform.position = clockAudioPoint != null
                ? clockAudioPoint.position
                : new Vector3(3.55f, 1.35f, 2.7f);

            clockTickSource = clockAudioObject.AddComponent<AudioSource>();
            clockTickSource.clip = clockTickClip;
            clockTickSource.loop = true;
            clockTickSource.playOnAwake = false;
            clockTickSource.spatialBlend = 1f;
            clockTickSource.rolloffMode = AudioRolloffMode.Linear;
            clockTickSource.minDistance = 1.2f;
            clockTickSource.maxDistance = 8f;
            clockTickSource.dopplerLevel = 0f;
            clockTickSource.volume = clockTickVolume;
            clockTickSource.Play();
        }
    }

    private void UpdateDoorAudioPause(bool shouldPause)
    {
        if (doorAudioPaused == shouldPause) return;

        if (shouldPause)
        {
            roomToneSource?.Pause();
            clockTickSource?.Pause();
        }
        else
        {
            roomToneSource?.UnPause();
            clockTickSource?.UnPause();
        }

        doorAudioPaused = shouldPause;
    }

    private void CreateRobotStatusPanel()
    {
        Canvas canvas = label.canvas;
        if (canvas == null) return;

        robotStatusPanel = new GameObject("RobotStatusPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        robotStatusPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = (RectTransform)robotStatusPanel.transform;
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.sizeDelta = new Vector2(260f, 54f);
        panelRect.anchoredPosition = new Vector2(-24f, -84f);

        Image panelImage = robotStatusPanel.GetComponent<Image>();
        panelImage.color = new Color(0.035f, 0.055f, 0.075f, 0.88f);
        panelImage.raycastTarget = false;

        GameObject dotObject = new GameObject("StatusDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dotObject.transform.SetParent(robotStatusPanel.transform, false);
        RectTransform dotRect = (RectTransform)dotObject.transform;
        dotRect.anchorMin = new Vector2(0f, 0.5f);
        dotRect.anchorMax = new Vector2(0f, 0.5f);
        dotRect.pivot = new Vector2(0f, 0.5f);
        dotRect.anchoredPosition = new Vector2(16f, 0f);
        dotRect.sizeDelta = new Vector2(12f, 12f);
        robotStatusDot = dotObject.GetComponent<Image>();
        robotStatusDot.color = new Color(0.3f, 0.78f, 1f);
        robotStatusDot.raycastTarget = false;

        GameObject textObject = new GameObject("RobotStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(robotStatusPanel.transform, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(38f, 5f);
        textRect.offsetMax = new Vector2(-10f, -5f);
        robotStatusLabel = textObject.GetComponent<TextMeshProUGUI>();
        robotStatusLabel.font = label.font;
        robotStatusLabel.fontSize = 15f;
        robotStatusLabel.color = Color.white;
        robotStatusLabel.alignment = TextAlignmentOptions.MidlineLeft;
        robotStatusLabel.raycastTarget = false;
    }

    private void UpdateRobotStatus(bool showHud)
    {
        if (robotStatusPanel == null) return;
        if (robotController == null)
            robotController = FindAnyObjectByType<RobotController>();

        bool show = showHud && robotController != null;
        if (robotStatusPanel.activeSelf != show)
            robotStatusPanel.SetActive(show);
        if (!show) return;

        string stateText;
        Color stateColor;
        if (!robotController.IsOperational)
        {
            stateText = "Devre dışı";
            stateColor = new Color(0.65f, 0.68f, 0.72f);
        }
        else
        {
            switch (robotController.CurrentState)
            {
                case RobotController.RobotState.MoveToTarget:
                    stateText = "Hedefe ilerliyor";
                    stateColor = new Color(0.32f, 0.78f, 1f);
                    break;
                case RobotController.RobotState.ScanInteract:
                    stateText = "Şalteri tarıyor";
                    stateColor = new Color(1f, 0.68f, 0.28f);
                    break;
                case RobotController.RobotState.FollowPlayer:
                    stateText = "Oyuncuyu takip ediyor";
                    stateColor = new Color(0.34f, 0.9f, 0.76f);
                    break;
                default:
                    stateText = "Beklemede";
                    stateColor = new Color(0.84f, 0.86f, 0.9f);
                    break;
            }
        }

        robotStatusLabel.text = $"ROBOT  ·  {stateText}";
        robotStatusDot.color = stateColor;
    }

    private void ConfigureClockLabel()
    {
        var rect = label.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(160f, 52f);
        rect.anchoredPosition = new Vector2(-24f, -24f);
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 30f;
        label.color = Color.white;
    }

    private void CreateObjectivePanel()
    {
        var panelTransform = label.transform.parent;
        objectivePanel = new GameObject("ObjectivePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        objectivePanel.transform.SetParent(panelTransform, false);

        var panelRect = (RectTransform)objectivePanel.transform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(420f, 108f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);

        var background = objectivePanel.GetComponent<RawImage>();
        background.texture = Texture2D.whiteTexture;
        background.color = new Color(0.035f, 0.055f, 0.075f, 0.88f);
        background.raycastTarget = false;

        var textObject = new GameObject("ObjectiveText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(objectivePanel.transform, false);
        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 12f);
        textRect.offsetMax = new Vector2(-18f, -12f);

        objectiveLabel = textObject.GetComponent<TextMeshProUGUI>();
        objectiveLabel.font = label.font;
        objectiveLabel.fontSize = 18f;
        objectiveLabel.color = Color.white;
        objectiveLabel.alignment = TextAlignmentOptions.MidlineLeft;
        objectiveLabel.raycastTarget = false;
    }

    private void CreateNotification()
    {
        var canvas = label.canvas;
        if (canvas == null) return;

        var panel = new GameObject("GameNotification", typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(560f, 56f);
        rect.anchoredPosition = new Vector2(0f, 92f);

        var image = panel.GetComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.035f, 0.055f, 0.075f, 0.94f);
        image.raycastTarget = false;
        notificationGroup = panel.GetComponent<CanvasGroup>();
        notificationGroup.alpha = 0f;

        var textObject = new GameObject("NotificationText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 6f);
        textRect.offsetMax = new Vector2(-16f, -6f);
        notificationLabel = textObject.GetComponent<TextMeshProUGUI>();
        notificationLabel.font = label.font;
        notificationLabel.fontSize = 20f;
        notificationLabel.color = Color.white;
        notificationLabel.alignment = TextAlignmentOptions.Center;
        notificationLabel.raycastTarget = false;
    }

    private void ShowNotification(string message)
    {
        if (notificationLabel == null || notificationGroup == null) return;
        notificationLabel.text = message;
        if (notificationRoutine != null) StopCoroutine(notificationRoutine);
        notificationRoutine = StartCoroutine(NotificationFade());
    }

    private IEnumerator NotificationFade()
    {
        notificationGroup.alpha = 1f;
        yield return new WaitForSeconds(2.2f);
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            notificationGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.35f);
            yield return null;
        }
        notificationGroup.alpha = 0f;
        notificationRoutine = null;
    }

    private static string GetObjectiveText(GameManager gm)
    {
        switch (gm.StoryStage)
        {
            case LibraryStoryStage.WakeAtDesk:
                return "VİZE HAFTASI  ·  ÜST KAT\nMasadan E ile kalk ve robotu ara.";
            case LibraryStoryStage.FindRobot:
                return "GÖREV 1  ·  ROBOTU BUL\nMavi ışığı takip et; yakınına gelince E ile etkinleştir.";
            case LibraryStoryStage.ActivateRobot:
                return $"GÖREV 2  ·  ROBOTU ÇALIŞTIR\nTozu temizle ve E komutunu tekrarla.  {gm.RobotActivationProgress}/{gm.RobotActivationRequired}";
            case LibraryStoryStage.DescendStairs:
                return "GÖREV 3  ·  ALT KATA İN\nRobotu takip et; merdivenlerden dikkatle in.";
            case LibraryStoryStage.ClearDebris:
                return $"GÖREV 4  ·  GEÇİŞİ AÇ\nDevrilmiş raflara bakıp E ile robotu yönlendir.  {gm.ClearedDebrisCount}/3";
            case LibraryStoryStage.ReachSecurity:
                return "GÖREV 5  ·  GÜVENLİK GÖREVLİSİ\nÇıkışta görevliye yaklaş ve E ile konuş.";
            case LibraryStoryStage.PersuadeSecurity:
                return "SON GÖREV  ·  İKNA ET\nResmî dilekçeni yaz ve güvenliğe gönder.";
            case LibraryStoryStage.ReachExit:
                return "SON GÖREV  ·  ÇIKIŞA İLERLE\nKapı açıldı; çıkıştan geçerek kaçışı tamamla.";
            case LibraryStoryStage.Complete:
                return "KAÇIŞ TAMAMLANDI\nGüvenlik ikna oldu; kütüphane kapısı açıldı.";
            default:
                return "GECE NÖBETİ  ·  KÜTÜPHANE";
        }
    }
}
