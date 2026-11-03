using UnityEngine;
using Breakpoint.Core;
using Breakpoint.Robot;

public enum LibraryInteractionKind
{
    WakeFromDesk,
    ActivateRobot,
    ClearDebris
}

/// <summary>Coordinates story interactions while leaving navigation to the robot's NavMeshAgent.</summary>
[RequireComponent(typeof(Collider))]
public sealed class LibraryStoryInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private LibraryInteractionKind kind;
    [SerializeField] private string prompt = "E · Etkileşim";
    [SerializeField, Min(1)] private int requiredInteractions = 3;
    [SerializeField] private RobotController robot;
    [SerializeField] private Transform robotWorkPoint;
    [SerializeField] private GameObject debrisVisuals;

    private int interactionCount;
    private bool commandInProgress;
    private bool cleared;

    public string InteractionPrompt => prompt;
    public int InteractionCount => interactionCount;

    private void Awake()
    {
        if (robot == null) robot = FindFirstObjectByType<RobotController>();
        if (kind == LibraryInteractionKind.ActivateRobot && robot != null)
            robot.SetOperational(false);
    }

    private void Update()
    {
        if (!commandInProgress || robot == null || robotWorkPoint == null) return;
        if (!robot.HasReachedDestination(robotWorkPoint.position, 0.85f)) return;

        commandInProgress = false;
        cleared = true;
        if (debrisVisuals != null) debrisVisuals.SetActive(false);
        GameManager.Instance?.RegisterDebrisCleared();
        robot.FollowPlayer();
        GameEvents.RaiseNotification("Robot enkazı kaldırdı. Yol açılıyor.");
    }

    public void Configure(LibraryInteractionKind interactionKind, string interactionPrompt,
        int interactionSteps = 1, RobotController robotController = null,
        Transform workPoint = null, GameObject visuals = null)
    {
        kind = interactionKind;
        prompt = interactionPrompt;
        requiredInteractions = Mathf.Max(1, interactionSteps);
        robot = robotController;
        robotWorkPoint = workPoint;
        debrisVisuals = visuals;
    }

    public void Interact(PlayerController player)
    {
        switch (kind)
        {
            case LibraryInteractionKind.WakeFromDesk:
                WakeFromDesk(player);
                break;
            case LibraryInteractionKind.ActivateRobot:
                ActivateRobot();
                break;
            case LibraryInteractionKind.ClearDebris:
                CommandRobotToClearDebris();
                break;
        }
    }

    private void WakeFromDesk(PlayerController player)
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.StoryStage != LibraryStoryStage.WakeAtDesk)
            return;

        player.PlayWakeUpSequence();
        manager.SetStoryStage(LibraryStoryStage.FindRobot);
        GameEvents.RaiseNotification("Kalkıp çalışma masasının yanındaki robotu bul.");
    }

    private void ActivateRobot()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || robot == null) return;
        if (manager.StoryStage == LibraryStoryStage.FindRobot)
            manager.SetStoryStage(LibraryStoryStage.ActivateRobot);
        if (manager.StoryStage != LibraryStoryStage.ActivateRobot) return;

        interactionCount++;
        manager.SetRobotActivationProgress(interactionCount, requiredInteractions);
        if (interactionCount < requiredInteractions)
        {
            GameEvents.RaiseNotification(interactionCount == 1
                ? "Tozu temizle. E ile fırçalama komutunu tekrarla."
                : "Batarya bağlantısını sabitle. Bir kez daha E'ye bas.");
            return;
        }

        robot.SetOperational(true);
        manager.SetStoryStage(LibraryStoryStage.DescendStairs);
        GameEvents.RaiseNotification("Robot çalışıyor. Birlikte merdivenlerden alt kata inin.");
    }

    private void CommandRobotToClearDebris()
    {
        GameManager manager = GameManager.Instance;
        if (cleared || commandInProgress || manager == null || robot == null || robotWorkPoint == null)
            return;
        if (manager.StoryStage != LibraryStoryStage.ClearDebris)
        {
            GameEvents.RaiseNotification("Önce robotla birlikte alt kata in.");
            return;
        }
        if (!robot.IsOperational)
        {
            GameEvents.RaiseNotification("Robotu önce temizleyip yeniden çalıştır.");
            return;
        }

        if (!robot.MoveTo(robotWorkPoint.position))
        {
            GameEvents.RaiseNotification("Robot bu enkaza ulaşamıyor. Merdiven NavMesh bağlantısını kontrol et.");
            return;
        }

        commandInProgress = true;
        GameEvents.RaiseNotification("Robot enkazın yanına gidiyor; geçiş için alanı temizliyor.");
    }
}
