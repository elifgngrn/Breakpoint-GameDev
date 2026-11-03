using UnityEngine;
using Breakpoint.Robot;

/// <summary>Routes a nearby switch interaction to the companion robot.</summary>
[RequireComponent(typeof(Collider))]
public sealed class SwitchInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private RobotController robot;
    public string InteractionPrompt => "E · Robotu şaltere gönder";

    public void Interact(PlayerController player)
    {
        if (robot == null) robot = FindFirstObjectByType<RobotController>();
        bool accepted = robot != null && robot.MoveToSwitch(transform);
        Breakpoint.Core.GameEvents.RaiseNotification(accepted
            ? "Robot şaltere doğru ilerliyor."
            : "Robot şaltere ulaşamıyor; NavMesh bağlantısını kontrol edin.");
        Debug.Log(accepted ? "Robot şaltere gönderildi." : "Robot şaltere ulaşamadı; NavMesh ve bağlantıları kontrol edin.", this);
    }
}
