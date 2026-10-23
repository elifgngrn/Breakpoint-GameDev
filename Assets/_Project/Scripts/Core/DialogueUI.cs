using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Core;
using Breakpoint.Core;

public class DialogueUI : MonoBehaviour
{
    [Header("Arayüz Bağlantıları")]
    public GameObject dialoguePanel;
    public TMP_Text npcAnswerText;
    public TMP_InputField playerInputField;
    public Button sendButton;

    [Header("Yapay Zeka Bağlantısı")]
    public OllamaManager ollamaManager;

    private void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(MesajGonder);
        }

        DiyaloguBaslat();   // panel sahne açılınca zaten görünüyor, durumu da buna eşle
    }

    public void DiyaloguBaslat()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        GameEvents.RaiseDialogueStarted(); 
        
        // Oyuncunun hareketini ve kamerasını dondur, imleci serbest bırak
        FindAnyObjectByType<PlayerController>()?.SetDialogueActive(true);

        // Metin kutusuna otomatik odaklan
        if (playerInputField != null)
        {
            playerInputField.Select();
            playerInputField.ActivateInputField();
        }

        Debug.Log("Diyalog paneli açıldı, oyuncu donduruldu.");
    }

    public void DiyaloguBitir()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        GameEvents.RaiseDialogueEnded(); 
        
        // Oyuncuyu serbest bırak, imleci kilitle
        FindAnyObjectByType<PlayerController>()?.SetDialogueActive(false);

        Debug.Log("Diyalog paneli kapandı, oyuncu serbest.");
    }

    private async void MesajGonder()
    {
        string mesaj = playerInputField.text;
        if (string.IsNullOrWhiteSpace(mesaj)) return;

        // Çift tıklamayı önlemek için butonu geçici kapat
        sendButton.interactable = false;
        npcAnswerText.text = "Kütüphaneci dilekçeni okuyor...";

        // Mesajı Ollama'ya gönder ve yanıt bekle
        var yanit = await ollamaManager.SendPromptToOllama(mesaj);

        // Gelen yanıtı ekrana yazdır
        npcAnswerText.text = yanit.dialogue;

        if (!yanit.door_locked)
        {
            Debug.Log("Güvenlik ikna oldu, kapı açılıyor!");
            GameEvents.RaiseDoorUnlocked();   // sayaç durur
            FindAnyObjectByType<DoorController>().UnlockAndOpenDoor();
        }

        // Yeni mesaj yazılabilmesi için alanı temizle ve butonu aç
        playerInputField.text = "";
        sendButton.interactable = true;
    }
}