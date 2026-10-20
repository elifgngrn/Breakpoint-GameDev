using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI Bileşenleri")]
    [SerializeField] private TMP_InputField playerInputField;
    [SerializeField] private TMP_Text npcDialogueText;
    [SerializeField] private TMP_Text thinkingIndicatorText;
    [SerializeField] private Button sendButton;
[SerializeField] private DoorController doorController;
    [Header("Referanslar")]
    [SerializeField] private OllamaClient ollamaClient;

    private void Start()
{
    if (thinkingIndicatorText != null)
        thinkingIndicatorText.gameObject.SetActive(false);

    if (sendButton != null)
        sendButton.onClick.AddListener(OnSendButtonClicked);

    // Otomatik UI Düzenleme (Yazıların sığması ve hizalanması için)
    if (npcDialogueText != null)
    {
        npcDialogueText.fontSize = 18;
        npcDialogueText.enableAutoSizing = true; // Yazı kutuya otomatik sığar
        npcDialogueText.fontSizeMin = 14;
        npcDialogueText.fontSizeMax = 22;
    }
    
}
       

    public async void OnSendButtonClicked()
    {
        string userInput = playerInputField.text;

        if (string.IsNullOrWhiteSpace(userInput))
            return;

        SetUIState(isProcessing: true);

        NPCResponseData response = await ollamaClient.SendDialogueToNPC(userInput);

        SetUIState(isProcessing: false);

        if (response != null)
        {
            if (!response.door_locked && doorController != null)
{
if (!response.door_locked && doorController != null)
{
    doorController.OpenDoor(); // UnlockDoor yerine OpenDoor yazıyoruz!
}
}
            npcDialogueText.text = response.dialogue_response;
            playerInputField.text = ""; 
            
            Debug.Log($"[UI Güncellendi] Mood: {response.mood} | Kapı Kilitli mi: {response.door_locked}");
        }
        else
        {
            npcDialogueText.text = "<color=red>Kütüphaneci cevap vermedi... (Sunucu Hatası)</color>";
        }
    }

    private void SetUIState(bool isProcessing)
    {
        if (sendButton != null) sendButton.interactable = !isProcessing;
        if (playerInputField != null) playerInputField.interactable = !isProcessing;
        
        if (thinkingIndicatorText != null)
        {
            thinkingIndicatorText.gameObject.SetActive(isProcessing);
            if (isProcessing) thinkingIndicatorText.text = "Dilekçeniz inceleniyor...";
        }
    }
}