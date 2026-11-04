using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Breakpoint.Core;

/// <summary>
/// Oyun akışı: durumlar, sahne geçişi, zaman sayacı, imleç modu.
/// Hangi sahneden Play'e basılırsa basılsın otomatik oluşur (test kolaylığı).
/// </summary>
// Persistent game flow and scene coordination.
[RequireComponent(typeof(GameClock))]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Sahne Adları")]
    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private string corridorScene = "Level_Corridor";
    [SerializeField] private string doorScene = "Level_Door";

    private GameClock clock;
    private ICursorController cursor;

    public GameState State { get; private set; } = GameState.MainMenu;
    public GameClock Clock => clock;
    public bool LasersCleared { get; private set; }
    public bool DoorUnlocked { get; private set; }
    public LibraryStoryStage StoryStage { get; private set; } = LibraryStoryStage.WakeAtDesk;
    public int ClearedDebrisCount { get; private set; }
    public int RobotActivationProgress { get; private set; }
    public int RobotActivationRequired { get; private set; } = 3;
    public event Action<LibraryStoryStage> StoryStageChanged;
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
        if (GetComponent<PauseMenuUI>() == null)
            gameObject.AddComponent<PauseMenuUI>();

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

    private void Update()
    {
        bool keyboardPause = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepadPause = Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
        if (!keyboardPause && !gamepadPause) return;

        if (State == GameState.Playing)
            Pause();
        else if (State == GameState.Paused)
            Resume();
    }

    // ---- Menüden çağrılır ----
    public void StartNewGame()
    {
        LasersCleared = false;
        DoorUnlocked = false;
        ClearedDebrisCount = 0;
        RobotActivationProgress = 0;
        SetStoryStage(LibraryStoryStage.WakeAtDesk);
        clock.ResetClock();
        SceneManager.LoadScene(corridorScene);
        BeginPlaying();
    }

    public void Restart() => StartNewGame();

    public void Pause()
    {
        if (State == GameState.Playing)
            SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State == GameState.Paused)
            SetState(GameState.Playing);
    }

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
        if (State != GameState.Playing || StoryStage != LibraryStoryStage.ReachSecurity) return;
        SetStoryStage(LibraryStoryStage.PersuadeSecurity);
        SceneManager.LoadScene(doorScene);
    }

    public void SetStoryStage(LibraryStoryStage stage)
    {
        if (StoryStage == stage) return;
        StoryStage = stage;
        StoryStageChanged?.Invoke(stage);
    }

    public void SetRobotActivationProgress(int progress, int required)
    {
        RobotActivationRequired = Mathf.Max(1, required);
        RobotActivationProgress = Mathf.Clamp(progress, 0, RobotActivationRequired);
        StoryStageChanged?.Invoke(StoryStage);
    }

    public void RegisterDebrisCleared()
    {
        if (StoryStage != LibraryStoryStage.ClearDebris) return;
        ClearedDebrisCount = Mathf.Min(3, ClearedDebrisCount + 1);
        if (ClearedDebrisCount >= 3)
        {
            SetStoryStage(LibraryStoryStage.ReachSecurity);
            GameEvents.RaiseNotification("Yol açık. Koridorun sonundaki güvenliğe ilerle.");
        }
        else
        {
            StoryStageChanged?.Invoke(StoryStage);
        }
    }

    private void OnDialogueStarted()
    {
        if (State == GameState.Playing) SetState(GameState.Dialogue);
    }

    private void OnDialogueEnded()
    {
        if (State == GameState.Dialogue) SetState(GameState.Playing);
    }

    // Kapı açıldı; süre durur ama oyuncu çıkışa yürüyerek kaçışı tamamlar.
    private void OnDoorUnlocked()
    {
        DoorUnlocked = true;
        clock.StopClock();
        SetStoryStage(LibraryStoryStage.ReachExit);
        GameEvents.RaiseNotification("Kapı açıldı. Çıkışa ilerle!");
    }

    private void OnEscapeCompleted()
    {
        if (!DoorUnlocked || State != GameState.Playing) return;
        SetStoryStage(LibraryStoryStage.Complete);
        Finish(GameState.Won);
    }

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
            case GameState.Paused:
            case GameState.Won:
            case GameState.Lost:
                Time.timeScale = 0f;
                cursor.SetUIMode();
                break;
        }

        StateChanged?.Invoke(next);
    }
}
