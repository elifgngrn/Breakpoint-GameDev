/// <summary>Small contract for objects the player can activate with the interaction key.</summary>
public interface IInteractable
{
    string InteractionPrompt { get; }
    void Interact(PlayerController player);
}
