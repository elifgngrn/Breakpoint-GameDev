using UnityEngine;

/// <summary>Kazanma/kaybetme panelleri. Butonların OnClick'ine Restart / QuitToMenu bağlanır.</summary>
public sealed class EndScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private void Start()
    {
        Hide();
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameState state)
    {
        winPanel.SetActive(state == GameState.Won);
        losePanel.SetActive(state == GameState.Lost);
    }

    private void Hide()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    public void OnRestartClicked() => GameManager.Instance.Restart();
    public void OnMenuClicked()    => GameManager.Instance.QuitToMenu();
}
