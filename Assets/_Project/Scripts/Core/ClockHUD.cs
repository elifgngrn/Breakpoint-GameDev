using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using Breakpoint.Core;

public sealed class ClockHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    private TMP_Text objectiveLabel;
    private GameObject objectivePanel;
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
        CreateNotification();
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (label == null || gm == null || gm.Clock == null) return;

        bool showHud = gm.State != GameState.Won && gm.State != GameState.Lost;
        if (label.gameObject.activeSelf != showHud)
            label.gameObject.SetActive(showHud);
        if (objectivePanel != null && objectivePanel.activeSelf != showHud)
            objectivePanel.SetActive(showHud);
        if (!showHud) return;

        label.text = gm.Clock.Formatted;
        if (objectiveLabel != null)
            objectiveLabel.text = GetObjectiveText(gm);
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
            case LibraryStoryStage.Complete:
                return "KAÇIŞ TAMAMLANDI\nGüvenlik ikna oldu; kütüphane kapısı açıldı.";
            default:
                return "GECE NÖBETİ  ·  KÜTÜPHANE";
        }
    }
}
