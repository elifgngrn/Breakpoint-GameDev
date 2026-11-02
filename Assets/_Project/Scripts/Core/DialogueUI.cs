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

    [Header("Dilekçe Kuralları")]
    [SerializeField] private PetitionRules petitionRules;

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
        if (dialoguePanel == null)
        {
            Debug.LogError("DialogueUI: dialoguePanel Inspector'da atanmamış.", this);
            return;
        }
        dialoguePanel.SetActive(true);
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
        if (dialoguePanel == null)
        {
            Debug.LogError("DialogueUI: dialoguePanel Inspector'da atanmamış.", this);
            return;
        }
        dialoguePanel.SetActive(false);
        GameEvents.RaiseDialogueEnded(); 
        
        // Oyuncuyu serbest bırak, imleci kilitle
        FindAnyObjectByType<PlayerController>()?.SetDialogueActive(false);

        Debug.Log("Diyalog paneli kapandı, oyuncu serbest.");
    }

    private async void MesajGonder()
    {
        // Her referans kullanılmadan önce doğrulanır; eksik UI/servis oyunu düşürmez.
        if (playerInputField == null)
        {
            Debug.LogError("DialogueUI: playerInputField Inspector'da atanmamış.", this);
            return;
        }
        if (npcAnswerText == null)
        {
            Debug.LogError("DialogueUI: npcAnswerText Inspector'da atanmamış.", this);
            return;
        }
        if (ollamaManager == null)
        {
            Debug.LogError("DialogueUI: ollamaManager Inspector'da atanmamış.", this);
            return;
        }

        string mesaj = playerInputField.text;
        if (string.IsNullOrWhiteSpace(mesaj)) return;

        bool dilekceKabulEdildi = PetitionValidator.Validate(mesaj, petitionRules, out string dilekceGeriBildirimi);

    // Çift tıklamayı önlemek için butonu geçici kapat
        if (sendButton != null) sendButton.interactable = false;
        npcAnswerText.text = $"Kütüphaneci yanıtlıyor... (en fazla {ollamaManager.RequestTimeoutSeconds} sn)";

        // Kapı kararı yerel doğrulayıcıdan gelir; Ollama yanıtını beklemez.
        if (dilekceKabulEdildi)
        {
            var door = FindAnyObjectByType<DoorController>();
            if (door != null)
            {
                door.UnlockAndOpenDoor();
                GameEvents.RaiseDoorUnlocked();
            }
            else
            {
                Debug.LogWarning("DialogueUI: Sahne içinde DoorController bulunamadı; kapı açılamadı.", this);
            }
        }

    // Mesajı Ollama'ya gönder ve yanıt bekle
        var manager = ollamaManager;
        var yanit = await manager.SendPromptToOllama(mesaj, dilekceKabulEdildi);

        // Sahne değişmiş veya UI kapatılmış olabilirken async istek tamamlanabilir.
        if (this == null || npcAnswerText == null || playerInputField == null)
            return;

    // Yanıtın null gelme ihtimaline karşı güvenlik kontrolü
        if (yanit == null)
        {
            npcAnswerText.text = "Kütüphaneci şu an cevap veremiyor.";
            if (sendButton != null) sendButton.interactable = true;
            return;
        }

    // Gelen yanıtı ekrana yazdır
        string npcYaniti = string.IsNullOrEmpty(yanit.dialogue)
            ? "Kütüphaneci cevap verdi ancak mesajı alınamadı."
            : yanit.dialogue;
        npcYaniti = NormalizeQuotesForFont(npcYaniti);
        if (yanit.usedFallback)
            npcYaniti += "\n\nOllama yanıt veremedi; yerel yanıt gösteriliyor.";

        npcAnswerText.text = dilekceKabulEdildi
            ? npcYaniti
            : $"{npcYaniti}\n\n{dilekceGeriBildirimi}";

    // Yeni mesaj yazılabilmesi için alanı temizle ve butonu aç
        playerInputField.text = "";
        if (sendButton != null) sendButton.interactable = true;
    }

    private static string NormalizeQuotesForFont(string text)
    {
        return text
            .Replace("\u2018", "'")
            .Replace("\u2019", "'")
            .Replace("\u201C", "\"")
            .Replace("\u201D", "\"");
    }
}
