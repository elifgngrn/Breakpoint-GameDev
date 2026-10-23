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
        // Butona tıklandığında MesajGonder fonksiyonunu dinle
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(MesajGonder);
        }
    }

    public void DiyaloguBaslat()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        GameEvents.RaiseDialogueStarted(); 
        Debug.Log("Diyalog paneli açıldı, oyuncu donduruldu.");
    }

    public void DiyaloguBitir()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        GameEvents.RaiseDialogueEnded(); 
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

        // Eğer yapay zeka ikna olduysa (kapıyı açmayı kabul ettiyse)
        if (!yanit.door_locked)
        {
            Debug.Log("Güvenlik ikna oldu, kapı açılıyor!");
            FindAnyObjectByType<DoorController>().UnlockAndOpenDoor();
        }
        else if (yanit.call_police)
        {
            Debug.Log("Güvenlik polisi aradı! (Buraya oyun bitirme ekranı eklenebilir)");
        }

        // Yeni mesaj yazılabilmesi için alanı temizle ve butonu aç
        playerInputField.text = "";
        sendButton.interactable = true;
    }
}