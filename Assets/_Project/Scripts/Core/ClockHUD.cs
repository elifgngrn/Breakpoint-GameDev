using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ClockHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    private TMP_Text objectiveLabel;
    private GameObject objectivePanel;

    private void Start()
    {
        if (label == null || label.transform.parent == null) return;

        ConfigureClockLabel();
        CreateObjectivePanel();
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
