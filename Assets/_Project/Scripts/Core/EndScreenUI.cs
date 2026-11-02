using System.Collections;
using UnityEngine;
using Breakpoint.Core;

public class EndScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private float fadeDuration = 0.5f; // Geçiş süresi

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
                ShowWinPanel();
                break;
            case GameState.Lost:
                ShowLosePanel();
                break;
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

        if (GUI.Button(new Rect(left, top, buttonWidth, buttonHeight), "TEKRAR OYNA"))
            gameManager.Restart();

        if (GUI.Button(new Rect(left + buttonWidth + gap, top, buttonWidth, buttonHeight), "ANA MENÜ"))
            gameManager.QuitToMenu();
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
