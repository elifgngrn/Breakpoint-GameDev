using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;

/// <summary>
/// Oyun akışı: durumlar, sahne geçişi, zaman sayacı, imleç modu.
/// Hangi sahneden Play'e basılırsa basılsın otomatik oluşur (test kolaylığı).
/// </summary>
[RequireComponent(typeof(GameClock))]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private string corridorScene = "Level_Corrider";
    [SerializeField] private string doorScene = "Level_Door";

    private GameClock clock;
    private ICursorController cursor;

    public GameState State { get; private set; } = GameState.MainMenu;
    public GameClock Clock => clock;
    public bool LasersCleared { get; private set; }
    public event Action<GameState> StateChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>(); // RequireComponent GameClock'u da ekler
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        clock = GetComponent<GameClock>();
        cursor = new CursorController();

        // Menü sahnesi yoksa / başka sahneden başlatıldıysa direkt oyuna gir
        bool inMenu = SceneManager.GetActiveScene().name == menuScene;
        if (inMenu) SetState(GameState.MainMenu);
        else BeginPlaying();
    }

    private void OnEnable()
    {
        GameEvents.LasersDisabled      += OnLasersDisabled;
        GameEvents.CorridorExitReached += OnCorridorExitReached;
        GameEvents.DialogueStarted     += OnDialogueStarted;
        GameEvents.DialogueEnded       += OnDialogueEnded;
        GameEvents.DoorUnlocked        += OnDoorUnlocked;
        GameEvents.EscapeCompleted     += OnEscapeCompleted;
        GameEvents.TimeExpired         += OnTimeExpired;
    }

    private void OnDisable()
    {
        GameEvents.LasersDisabled      -= OnLasersDisabled;
        GameEvents.CorridorExitReached -= OnCorridorExitReached;
        GameEvents.DialogueStarted     -= OnDialogueStarted;
        GameEvents.DialogueEnded       -= OnDialogueEnded;
        GameEvents.DoorUnlocked        -= OnDoorUnlocked;
        GameEvents.EscapeCompleted     -= OnEscapeCompleted;
        GameEvents.TimeExpired         -= OnTimeExpired;
    }

    // ---- Menüden çağrılır ----
    public void StartNewGame()
    {
        LasersCleared = false;
        clock.ResetClock();
        SceneManager.LoadScene(corridorScene);
        BeginPlaying();
    }

    public void Restart() => StartNewGame();

    public void QuitToMenu()
    {
        clock.ResetClock();
        SceneManager.LoadScene(menuScene);
        SetState(GameState.MainMenu);
    }

    // ---- Olay işleyicileri ----
    private void OnLasersDisabled() => LasersCleared = true;

    private void OnCorridorExitReached()
    {
        if (State != GameState.Playing || !LasersCleared) return;
        SceneManager.LoadScene(doorScene);
    }

    private void OnDialogueStarted()
    {
        if (State == GameState.Playing) SetState(GameState.Dialogue);
    }

    private void OnDialogueEnded()
    {
        if (State == GameState.Dialogue) SetState(GameState.Playing);
    }

    // Kapı açıldı: kaçış sekansı sürerken süre işlemesin
    private void OnDoorUnlocked() => clock.StopClock();

    private void OnEscapeCompleted() => Finish(GameState.Won);

    private void OnTimeExpired()
    {
        if (State == GameState.Playing || State == GameState.Dialogue)
            Finish(GameState.Lost);
    }

    // ---- Yardımcılar ----
    private void BeginPlaying()
    {
        clock.StartClock();
        SetState(GameState.Playing);
    }

    private void Finish(GameState result)
    {
        clock.StopClock();
        SetState(result);
    }

    private void SetState(GameState next)
    {
        State = next;

        switch (next)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                cursor.SetGameplayMode();
                break;
            case GameState.Dialogue:
            case GameState.MainMenu:
                Time.timeScale = 1f;
                cursor.SetUIMode();
                break;
            case GameState.Won:
            case GameState.Lost:
                Time.timeScale = 0f;
                cursor.SetUIMode();
                break;
        }

        StateChanged?.Invoke(next);
    }
}
