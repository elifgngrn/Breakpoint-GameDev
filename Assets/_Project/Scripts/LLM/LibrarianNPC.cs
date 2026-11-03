using UnityEngine;
using Breakpoint.Core;
using UnityEngine.SceneManagement;

/// <summary>World interaction entry point for the librarian's existing petition dialogue UI.</summary>
public sealed class LibrarianNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private string interactionPrompt = "E · Güvenlikle konuş";
    public string InteractionPrompt => interactionPrompt;
    private bool dialogueStarted;

    private void Awake()
    {
        if (dialogueUI == null) dialogueUI = FindFirstObjectByType<DialogueUI>();
    }

    public void Interact(PlayerController player)
    {
        // In the corridor the guard is the exit checkpoint. Let the existing
        // story event load the dedicated dialogue scene after the debris is clear.
        if (GameManager.Instance != null &&
            GameManager.Instance.StoryStage == LibraryStoryStage.ReachSecurity &&
            SceneManager.GetActiveScene().name == "Level_Corridor")
        {
            GameEvents.RaiseCorridorExitReached();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.StoryStage != LibraryStoryStage.PersuadeSecurity)
        {
            string message = GameManager.Instance.StoryStage == LibraryStoryStage.ClearDebris
                ? $"Önce geçişi aç: {GameManager.Instance.ClearedDebrisCount}/3 engel temizlendi."
                : "Güvenlik noktasına ulaşmadan önce üst kattaki görevleri tamamla.";
            GameEvents.RaiseNotification(message);
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogWarning("Security interaction: DialogueUI sahnede bulunamadı.", this);
            return;
        }

        OpenDialogue(forceReopen: true);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryOpenForNearbyPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Retry while the player remains close in case scene/UI initialization finished
        // one frame after the guard trigger was entered.
        TryOpenForNearbyPlayer(other);
    }

    private void TryOpenForNearbyPlayer(Collider other)
    {
        if (!other.CompareTag("Player") || dialogueStarted || GameManager.Instance == null ||
            GameManager.Instance.StoryStage != LibraryStoryStage.PersuadeSecurity)
            return;

        if (dialogueUI == null) dialogueUI = FindFirstObjectByType<DialogueUI>();
        OpenDialogue();
    }

    private void OpenDialogue(bool forceReopen = false)
    {
        if (dialogueStarted && !forceReopen) return;
        if (dialogueUI == null) dialogueUI = FindFirstObjectByType<DialogueUI>();
        if (dialogueUI == null)
        {
            Debug.LogWarning("Security interaction: DialogueUI sahnede bulunamadı.", this);
            return;
        }

        dialogueStarted = true;
        dialogueUI.DiyaloguBaslat();
    }
}
