using UnityEngine;

/// <summary>Kazanma/kaybetme panelleri. Butonların OnClick'ine Restart / QuitToMenu bağlanır.</summary>
public sealed class EndScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private GameManager subscribedManager;

    private void Start()
    {
        Hide();
        subscribedManager = GameManager.Instance;
        if (subscribedManager == null) return;
        subscribedManager.StateChanged += OnStateChanged;
        OnStateChanged(subscribedManager.State);
    }

    private void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        if (winPanel != null) winPanel.SetActive(state == GameState.Won);
        if (losePanel != null) losePanel.SetActive(state == GameState.Lost);
    }

    private void Hide()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    public void OnRestartClicked()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.Restart();
    }

    public void OnMenuClicked()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.QuitToMenu();
    }
}
