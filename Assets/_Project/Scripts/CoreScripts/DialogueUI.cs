using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Core;
using Breakpoint.Core;
using UnityEngine.InputSystem;

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
    private TMP_Text controlsHint;
    private bool greetingRequested;
    private bool requestPending;
    private int responseVersion;
    private Coroutine waitingRoutine;
    private Coroutine dialogueRevealRoutine;
    private AudioSource feedbackAudio;
    private AudioClip rejectionCue;

    private void Awake()
    {
        // A duplicate DialogueUI was serialized on DialoguePanel itself. It would
        // run Start only after the panel opened, then hide its own panel and leave
        // the game in Dialogue state. The Canvas-level component is the controller.
        if (dialoguePanel == gameObject)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        feedbackAudio = GetComponent<AudioSource>();
        if (feedbackAudio == null) feedbackAudio = gameObject.AddComponent<AudioSource>();
        feedbackAudio.playOnAwake = false;
        feedbackAudio.spatialBlend = 0f;
        rejectionCue = CreateRejectionCue();

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

        CreateControlsHint();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (dialoguePanel == null || !dialoguePanel.activeInHierarchy) return;

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        bool closePressed = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
        if (!requestPending && closePressed)
        {
            DiyaloguBitir();
            return;
        }

        bool submitPressed = (keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) ||
            (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        if (!requestPending && submitPressed)
        {
            if (dialogueRevealRoutine != null)
            {
                RevealCurrentTextImmediately();
                return;
            }

            if (playerInputField != null && !string.IsNullOrWhiteSpace(playerInputField.text))
                MesajGonder();
        }
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
        rect.anchoredPosition = new Vector2(0f, -82f);
        rect.sizeDelta = new Vector2(720f, 22f);

        petitionHint = hintObject.GetComponent<TextMeshProUGUI>();
        petitionHint.font = npcAnswerText != null
            ? npcAnswerText.font
            : playerInputField.textComponent != null ? playerInputField.textComponent.font : null;
        petitionHint.fontSize = 13f;
        petitionHint.alignment = TextAlignmentOptions.Center;
        petitionHint.raycastTarget = false;
    }

    private void CreateControlsHint()
    {
        if (dialoguePanel == null) return;

        var hintObject = new GameObject("DialogueControlsHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintObject.transform.SetParent(dialoguePanel.transform, false);
        var rect = (RectTransform)hintObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -102f);
        rect.sizeDelta = new Vector2(720f, 14f);

        controlsHint = hintObject.GetComponent<TextMeshProUGUI>();
        controlsHint.font = npcAnswerText != null
            ? npcAnswerText.font
            : playerInputField != null && playerInputField.textComponent != null ? playerInputField.textComponent.font : null;
        controlsHint.fontSize = 11f;
        controlsHint.alignment = TextAlignmentOptions.Center;
        controlsHint.color = new Color(0.72f, 0.8f, 0.84f, 0.85f);
        controlsHint.text = "Enter / A: Yazıyı göster · Tekrar: Gönder     Esc / B: Kapat";
        controlsHint.raycastTarget = false;
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

        if (!greetingRequested)
        {
            greetingRequested = true;
            _ = GreetPlayerAsync();
        }

        // Metin kutusuna otomatik odaklan
        if (playerInputField != null)
        {
            playerInputField.Select();
            playerInputField.ActivateInputField();
        }

        Debug.Log("Diyalog paneli açıldı, oyuncu donduruldu.");
    }

    private async System.Threading.Tasks.Task GreetPlayerAsync()
    {
        if (npcAnswerText == null) return;
        int version = responseVersion;
        npcAnswerText.text = "Güvenlik görevlisi düşünüyor...";
        if (ollamaManager == null)
        {
            ShowNpcText("İyi akşamlar. Çıkış için dilekçenizi iletir misiniz?");
            return;
        }

        NPCResponseData response = await ollamaManager.GenerateGreetingAsync();
        if (this == null || version != responseVersion || npcAnswerText == null || dialoguePanel == null || !dialoguePanel.activeInHierarchy)
            return;
        string greeting = response == null || string.IsNullOrWhiteSpace(response.dialogue)
            ? "İyi akşamlar. Çıkış için dilekçenizi iletir misiniz?"
            : NormalizeQuotesForFont(response.dialogue) +
              (response.usedFallback ? "\n\nYerel yanıt gösteriliyor." : string.Empty);
        ShowNpcText(greeting);
    }

    public void DiyaloguBitir()
    {
        if (dialoguePanel == null)
        {
            Debug.LogError("DialogueUI: dialoguePanel Inspector'da atanmamış.", this);
            return;
        }
        if (dialogueRevealRoutine != null)
        {
            StopCoroutine(dialogueRevealRoutine);
            dialogueRevealRoutine = null;
        }
        dialoguePanel.SetActive(false);
        GameEvents.RaiseDialogueEnded(); 
        
        // Oyuncuyu serbest bırak, imleci kilitle
        FindAnyObjectByType<PlayerController>()?.SetDialogueActive(false);

        Debug.Log("Diyalog paneli kapandı, oyuncu serbest.");
    }

    private async void MesajGonder()
    {
        if (requestPending) return;

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
        string mesaj = playerInputField.text;
        if (string.IsNullOrWhiteSpace(mesaj)) return;
        int requestVersion = ++responseVersion;

        bool yerelOnKontrolGecti = PetitionValidator.Validate(mesaj, petitionRules, out string dilekceGeriBildirimi);

    // Çift tıklamayı önlemek için butonu geçici kapat
        requestPending = true;
        if (dialogueRevealRoutine != null)
        {
            StopCoroutine(dialogueRevealRoutine);
            dialogueRevealRoutine = null;
        }
        npcAnswerText.maxVisibleCharacters = int.MaxValue;
        if (sendButton != null) sendButton.interactable = false;
        playerInputField.interactable = false;
        int timeout = ollamaManager != null ? ollamaManager.RequestTimeoutSeconds : 0;
        waitingRoutine = StartCoroutine(AnimateWaiting(requestVersion, timeout));

        // Yapılandırılmış LLM kararı yerel dilekçe kurallarıyla ayrıca doğrulanır.
        var manager = ollamaManager;
        NPCResponseData yanit = null;
        try
        {
            if (manager != null)
                yanit = await manager.SendPromptToOllama(mesaj, yerelOnKontrolGecti, dilekceGeriBildirimi);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Diyalog isteği tamamlanamadı: {exception.Message}", this);
        }

        // Sahne değişmiş veya UI kapatılmış olabilirken async istek tamamlanabilir.
        if (this == null || requestVersion != responseVersion || npcAnswerText == null || playerInputField == null)
            return;

        if (waitingRoutine != null) StopCoroutine(waitingRoutine);
        waitingRoutine = null;
        requestPending = false;
        playerInputField.interactable = true;

    // Yanıtın null gelme ihtimaline karşı güvenlik kontrolü
        if (yanit == null)
        {
            npcAnswerText.text = "Kütüphaneciye ulaşılamadı. Bağlantıyı kontrol edip yeniden deneyebilirsin.";
            if (sendButton != null) sendButton.interactable = true;
            PlayDecisionFeedback(false);
            return;
        }

    // Gelen yanıtı ekrana yazdır
        string npcYaniti = string.IsNullOrEmpty(yanit.dialogue)
            ? "Güvenlik görevlisi cevap verdi ancak mesajı alınamadı."
            : yanit.dialogue;
        bool unlockRequested = !yanit.usedFallback && yanit.doorDecision == "unlock";
        bool doorUnlocked = false;
        if (unlockRequested && !yerelOnKontrolGecti)
        {
            Debug.LogWarning("DialogueUI: Ollama unlock kararı verdi ancak dilekçe yerel kuralları karşılamadığı için kapı kilitli kaldı.", this);
            npcYaniti = "Dilekçe koşulları tamamlanmadığı için kapı kilitli kaldı.";
        }
        npcYaniti = NormalizeQuotesForFont(npcYaniti);
        if (yanit.usedFallback)
            npcYaniti += "\n\nOllama yanıt veremedi; yerel yanıt gösteriliyor.";

        if (unlockRequested && yerelOnKontrolGecti)
        {
            var door = FindAnyObjectByType<DoorController>();
            if (door != null)
            {
                door.UnlockAndOpenDoor();
                GameEvents.RaiseDoorUnlocked();
                doorUnlocked = true;
            }
            else
            {
                Debug.LogWarning("DialogueUI: Sahne içinde DoorController bulunamadı; kapı açılamadı.", this);
                npcYaniti += "\n\nKapı mekanizmasına ulaşılamadı.";
            }
        }

        string finalResponse = !doorUnlocked && !yerelOnKontrolGecti && !yanit.usedFallback
            ? $"{npcYaniti}\n\n{dilekceGeriBildirimi}"
            : npcYaniti;
        ShowNpcText(finalResponse);
        PlayDecisionFeedback(doorUnlocked);
        if (!doorUnlocked)
            GameEvents.RaiseNotification(yanit.usedFallback
                ? "Kütüphaneci yanıt veremedi. Dilekçeni tekrar gönderebilirsin."
                : "İzin reddedildi. Dilekçeni düzenleyip tekrar deneyebilirsin.");

        // Reddedilen veya fallback yanıtı alan dilekçe düzenlenip yeniden gönderilebilir.
        if (doorUnlocked)
            playerInputField.text = string.Empty;
        playerInputField.interactable = !doorUnlocked;
        if (sendButton != null) sendButton.interactable = !doorUnlocked;

        if (doorUnlocked)
            StartCoroutine(CloseDialogueAfterUnlock());
    }

    private IEnumerator AnimateWaiting(int request, int timeoutSeconds)
    {
        float elapsed = 0f;
        int dots = 0;
        while (requestPending && request == responseVersion)
        {
            dots = (dots + 1) % 4;
            string duration = timeoutSeconds > 0 ? $" · {Mathf.Max(0, timeoutSeconds - Mathf.FloorToInt(elapsed))} sn" : string.Empty;
            npcAnswerText.text = $"Kütüphaneci yanıt hazırlıyor{new string('.', dots)}{duration}";
            elapsed += 0.35f;
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }

    private void ShowNpcText(string text)
    {
        if (npcAnswerText == null) return;
        if (dialogueRevealRoutine != null)
            StopCoroutine(dialogueRevealRoutine);

        npcAnswerText.text = text;
        npcAnswerText.maxVisibleCharacters = 0;
        dialogueRevealRoutine = StartCoroutine(RevealNpcText());
    }

    private IEnumerator RevealNpcText()
    {
        const float charactersPerSecond = 48f;
        npcAnswerText.ForceMeshUpdate();
        int characterCount = npcAnswerText.textInfo.characterCount;
        if (characterCount == 0)
        {
            npcAnswerText.maxVisibleCharacters = int.MaxValue;
            dialogueRevealRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (npcAnswerText.maxVisibleCharacters < characterCount)
        {
            elapsed += Time.unscaledDeltaTime;
            npcAnswerText.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.CeilToInt(elapsed * charactersPerSecond));
            yield return null;
        }

        npcAnswerText.maxVisibleCharacters = int.MaxValue;
        dialogueRevealRoutine = null;
    }

    private void RevealCurrentTextImmediately()
    {
        if (dialogueRevealRoutine != null)
        {
            StopCoroutine(dialogueRevealRoutine);
            dialogueRevealRoutine = null;
        }

        if (npcAnswerText != null)
            npcAnswerText.maxVisibleCharacters = int.MaxValue;
    }

    private void PlayDecisionFeedback(bool accepted)
    {
        if (npcAnswerText == null) return;
        StopCoroutine(nameof(PulseDecisionText));
        StartCoroutine(PulseDecisionText(accepted));
        if (!accepted && feedbackAudio != null && rejectionCue != null)
            feedbackAudio.PlayOneShot(rejectionCue, 0.45f);
    }

    private IEnumerator PulseDecisionText(bool accepted)
    {
        Color original = npcAnswerText.color;
        Color accent = accepted ? new Color(0.45f, 1f, 0.67f) : new Color(1f, 0.62f, 0.38f);
        Transform textTransform = npcAnswerText.transform;
        Vector3 originalScale = textTransform.localScale;
        float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            npcAnswerText.color = Color.Lerp(accent, original, t);
            textTransform.localScale = Vector3.Lerp(originalScale * 1.035f, originalScale, t);
            yield return null;
        }
        npcAnswerText.color = original;
        textTransform.localScale = originalScale;
    }

    private static AudioClip CreateRejectionCue()
    {
        const int sampleRate = 22050;
        const float duration = 0.24f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float frequency = time < 0.12f ? 220f : 174.61f;
            float localTime = time < 0.12f ? time : time - 0.12f;
            float envelope = Mathf.Min(1f, localTime / 0.012f) * Mathf.Clamp01((duration - time) / 0.035f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.2f;
        }

        AudioClip clip = AudioClip.Create("PetitionRejectedCue", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private IEnumerator CloseDialogueAfterUnlock()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        while (dialogueRevealRoutine != null)
            yield return null;
        yield return new WaitForSecondsRealtime(1f);
        if (this != null && dialoguePanel != null && dialoguePanel.activeSelf)
            DiyaloguBitir();
    }

    private static string NormalizeQuotesForFont(string text)
    {
        return text
            .Replace("\u2018", "'")
            .Replace("\u2019", "'")
            .Replace("\u201C", "\"")
            .Replace("\u201D", "\"")
            // The project's LiberationSans SDF lacks these Polish letters that
            // occasionally appear in local model output.
            .Replace("\u0116", "E")
            .Replace("\u0117", "e")
            .Replace("\u0118", "E")
            .Replace("\u0119", "e");
    }
}
