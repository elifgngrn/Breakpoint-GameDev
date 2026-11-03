using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Breakpoint.Core;

public class EndScreenUI : MonoBehaviour
{
    private const string BestEscapeTimeKey = "Breakpoint.BestEscapeTimeSeconds";
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private float fadeDuration = 0.5f; // Geçiş süresi
    private int selectedEndOption;
    private GUIStyle resultStyle;
    private bool newRecordAchieved;

    private void Update()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || (gameManager.State != GameState.Won && gameManager.State != GameState.Lost) || Gamepad.current == null)
            return;

        if (Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.dpad.right.wasPressedThisFrame ||
            Gamepad.current.leftStick.left.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame)
            selectedEndOption = 1 - selectedEndOption;

        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            if (selectedEndOption == 0) gameManager.Restart();
            else gameManager.QuitToMenu();
        }
        else if (Gamepad.current.buttonEast.wasPressedThisFrame)
            gameManager.QuitToMenu();
    }

    private void Start()
    {
        HideAllPanels();
        SubscribeToGameManager();

        // UI, GameManager ilk durum olayını yayınladıktan sonra etkinleşmiş olabilir.
        if (GameManager.Instance != null)
            HandleStateChanged(GameManager.Instance.State);
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged -= HandleStateChanged;
    }

    private void SubscribeToGameManager()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.StateChanged -= HandleStateChanged;
        GameManager.Instance.StateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Won:
                SaveBestEscapeTime();
                ShowWinPanel();
                break;
            case GameState.Lost:
                ShowLosePanel();
                break;
        }
    }

    private void SaveBestEscapeTime()
    {
        if (GameManager.Instance == null || GameManager.Instance.Clock == null) return;

        int currentTime = Mathf.FloorToInt(GameManager.Instance.Clock.ElapsedSeconds);
        int bestTime = PlayerPrefs.GetInt(BestEscapeTimeKey, int.MaxValue);
        if (currentTime < bestTime)
        {
            newRecordAchieved = true;
            PlayerPrefs.SetInt(BestEscapeTimeKey, currentTime);
            PlayerPrefs.Save();
        }
    }

    private void OnGUI()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || (gameManager.State != GameState.Won && gameManager.State != GameState.Lost))
            return;

        const float buttonWidth = 190f;
        const float buttonHeight = 48f;
        const float gap = 16f;
        float left = (Screen.width - buttonWidth * 2f - gap) * 0.5f;
        float top = Screen.height * 0.72f;

        if (gameManager.Clock != null)
        {
            if (resultStyle == null)
            {
                resultStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }
            string result = gameManager.State == GameState.Won ? "KAÇIŞ SÜRESİ" : "GEÇEN SÜRE";
            GUI.Label(new Rect(left, top - 58f, buttonWidth * 2f + gap, 42f),
                $"{result}: {gameManager.Clock.ElapsedFormatted}", resultStyle);

            if (gameManager.State == GameState.Won && newRecordAchieved)
                GUI.Label(new Rect(left, top - 128f, buttonWidth * 2f + gap, 28f), "YENİ REKOR!", resultStyle);

            int bestTime = PlayerPrefs.GetInt(BestEscapeTimeKey, int.MaxValue);
            if (bestTime != int.MaxValue)
            {
                string bestFormatted = $"{bestTime / 60:00}:{bestTime % 60:00}";
                GUI.Label(new Rect(left, top - 96f, buttonWidth * 2f + gap, 28f),
                    $"EN İYİ KAÇIŞ: {bestFormatted}", resultStyle);
            }
        }

        GUI.color = selectedEndOption == 0 ? new Color(0.65f, 1f, 0.8f) : Color.white;
        if (GUI.Button(new Rect(left, top, buttonWidth, buttonHeight), "TEKRAR OYNA"))
            gameManager.Restart();

        GUI.color = selectedEndOption == 1 ? new Color(0.65f, 1f, 0.8f) : Color.white;
        if (GUI.Button(new Rect(left + buttonWidth + gap, top, buttonWidth, buttonHeight), "ANA MENÜ"))
            gameManager.QuitToMenu();
        GUI.color = Color.white;
    }

    private void HideAllPanels()
    {
        if (winPanel != null) { SetPanelAlpha(winPanel, 0f); winPanel.SetActive(false); }
        if (losePanel != null) { SetPanelAlpha(losePanel, 0f); losePanel.SetActive(false); }
    }

    public void ShowWinPanel()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            StartCoroutine(FadeInPanel(winPanel));
        }
    }

    public void ShowLosePanel()
    {
        if (losePanel != null)
        {
            losePanel.SetActive(true);
            StartCoroutine(FadeInPanel(losePanel));
        }
    }

    private void SetPanelAlpha(GameObject panel, float alpha)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = alpha;
    }

    private IEnumerator FadeInPanel(GameObject panel)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Oyun durdurulsa bile çalışması için
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
