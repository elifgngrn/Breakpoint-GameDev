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
    private TMP_Text characterCounter;
    private TMP_Text petitionHint;

    private void Start()
    {
        if (playerInputField != null && petitionRules != null)
        {
            playerInputField.characterLimit = petitionRules.MaximumLength;
            CreateCharacterCounter();
            CreatePetitionHint();
            playerInputField.onValueChanged.AddListener(UpdateCharacterCounter);
            UpdateCharacterCounter(playerInputField.text);
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(MesajGonder);
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void CreateCharacterCounter()
    {
        var counterObject = new GameObject("CharacterCounter", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        counterObject.transform.SetParent(playerInputField.transform, false);
        var rect = (RectTransform)counterObject.transform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-8f, 2f);
        rect.sizeDelta = new Vector2(110f, 16f);

        characterCounter = counterObject.GetComponent<TextMeshProUGUI>();
        characterCounter.font = playerInputField.textComponent != null ? playerInputField.textComponent.font : null;
        characterCounter.fontSize = 10f;
        characterCounter.alignment = TextAlignmentOptions.BottomRight;
        characterCounter.color = new Color(0.68f, 0.78f, 0.78f, 0.9f);
        characterCounter.raycastTarget = false;
    }

    private void UpdateCharacterCounter(string currentText)
    {
        if (petitionRules == null) return;

        int count = string.IsNullOrEmpty(currentText) ? 0 : currentText.Length;
        if (characterCounter != null)
        {
            characterCounter.text = $"{count}/{petitionRules.MaximumLength}";
            characterCounter.color = count >= petitionRules.MaximumLength
                ? new Color(1f, 0.55f, 0.4f)
                : new Color(0.68f, 0.78f, 0.78f, 0.9f);
        }

        if (petitionHint != null)
        {
            bool valid = PetitionValidator.Validate(currentText, petitionRules, out string feedback);
            petitionHint.text = valid ? "Dilekçe koşulları tamam; gönderebilirsin." : feedback;
            petitionHint.color = valid ? new Color(0.35f, 0.9f, 0.55f) : new Color(1f, 0.72f, 0.35f);
        }
    }

    private void CreatePetitionHint()
    {
        if (dialoguePanel == null) return;

        var hintObject = new GameObject("PetitionHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(dialoguePanel.transform, false);
        var rect = (RectTransform)hintObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -91f);
        rect.sizeDelta = new Vector2(720f, 22f);

        petitionHint = hintObject.GetComponent<TextMeshProUGUI>();
        petitionHint.font = npcAnswerText != null
            ? npcAnswerText.font
            : playerInputField.textComponent != null ? playerInputField.textComponent.font : null;
        petitionHint.fontSize = 13f;
        petitionHint.alignment = TextAlignmentOptions.Center;
        petitionHint.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (playerInputField != null)
            playerInputField.onValueChanged.RemoveListener(UpdateCharacterCounter);
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
        npcAnswerText.text = $"Güvenlik görevlisi düşünüyor... (en fazla {ollamaManager.RequestTimeoutSeconds} sn)";

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
            npcAnswerText.text = "Güvenlik görevlisi şu an cevap veremiyor.";
            if (sendButton != null) sendButton.interactable = true;
            return;
        }

    // Gelen yanıtı ekrana yazdır
        string npcYaniti = string.IsNullOrEmpty(yanit.dialogue)
            ? "Güvenlik görevlisi cevap verdi ancak mesajı alınamadı."
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
