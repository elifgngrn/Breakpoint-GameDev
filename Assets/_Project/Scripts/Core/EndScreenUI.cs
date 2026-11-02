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

    private void OnEnable()
    {
        // Sütlaç'ın yazdığı GameClock zamanı bitirince GameEvents üzerinden bu olay tetiklenir
        GameEvents.OnTimeExpired += HandleTimeExpired;
    }

    private void OnDisable()
    {
        // Hafıza sızıntısını (memory leak) önlemek için aboneliği iptal ediyoruz
        GameEvents.OnTimeExpired -= HandleTimeExpired;
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

    private void HandleTimeExpired()
    {
        // Zaman dolduğunda kaybetme panelini ("Yakalandınız") aktif et
        losePanel.SetActive(true);
        Time.timeScale = 0f; // Oyunu dondur
    }

    private void Hide()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    public void OnRestartClicked() => GameManager.Instance.Restart();
    public void OnMenuClicked()    => GameManager.Instance.QuitToMenu();
}