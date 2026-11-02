using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        rect.sizeDelta = new Vector2(180f, 54f);
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
        panelRect.sizeDelta = new Vector2(500f, 116f);
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
        objectiveLabel.fontSize = 19f;
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
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(560f, 56f);
        rect.anchoredPosition = new Vector2(0f, -28f);

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
        string scene = SceneManager.GetActiveScene().name;

        if (scene == "Level_Corridor")
        {
            return gm.LasersCleared
                ? "HEDEF  ·  ÇIKIŞA ULAŞ\nLazerler kapalı. Koridorun sonundaki çıkışa git."
                : "HEDEF  ·  ŞALTERİ DEVRE DIŞI BIRAK\nŞaltere nişan alıp sol tıkla; robotu gönder.";
        }

        if (scene == "Level_Door")
        {
            return gm.DoorUnlocked
                ? "HEDEF  ·  ÇIKIŞTAN GEÇ\nKapı açıldı. Kaçışı tamamlamak için kapıdan ilerle."
                : "HEDEF  ·  KÜTÜPHANECİYİ İKNA ET\nResmi mazeretini yazıp dilekçeni gönder.";
        }

        return "HEDEF  ·  OYUN DEVAM EDİYOR";
    }
}
