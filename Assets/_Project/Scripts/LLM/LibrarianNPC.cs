using UnityEngine;
using Breakpoint.Core;

/// <summary>World interaction entry point for the librarian's existing petition dialogue UI.</summary>
public sealed class LibrarianNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private string interactionPrompt = "E · Güvenlikle konuş";
    public string InteractionPrompt => interactionPrompt;

    private void Awake()
    {
        if (dialogueUI == null) dialogueUI = FindFirstObjectByType<DialogueUI>();
    }

    public void Interact(PlayerController player)
    {
        if (GameManager.Instance != null && GameManager.Instance.StoryStage != LibraryStoryStage.PersuadeSecurity)
        {
            GameEvents.RaiseNotification("Güvenlik görevlisiyle konuşmadan önce geçişteki rafları temizle.");
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogWarning("Security interaction: DialogueUI sahnede bulunamadı.", this);
            return;
        }

        dialogueUI.DiyaloguBaslat();
    }
}
