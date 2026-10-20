using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class OllamaClient : MonoBehaviour
{
    [Header("API Ayarları")]
    [SerializeField] private string ollamaUrl = "http://localhost:11434/api/generate";
    [SerializeField] private string modelName = "llama3.2";

    [Header("UI Elemanları")]
    [SerializeField] private TMP_InputField playerInputField; 
    [SerializeField] private TMP_Text npcAnswerText;          
    [SerializeField] private Button sendButton;               

    [Header("Kapı ve Panel Referansları")]
    [SerializeField] private GameObject dialoguePanel;        
    [SerializeField] private DoorController doorController;  // Kırmızı çizgi verip vermediğini kontrol edin!
    // Bürokratik Kütüphaneci NPC System Prompt'u
    private readonly string systemPrompt = 
        "Sen Kütüphane Çıkış Kapısını koruyan, kurallara takıntılı ve bürokrasiden asla taviz vermeyen sert bir Nöbetçi Kütüphanecisin. " +
        "Saat gece 04:00 ve kapı KİLİTLİDİR. " +
        "GÖREVİN: Oyuncudan resmi, geçerli ve mantıklı bir dilekçe veya mazeret istemektir. " +
        "KURALLAR:\n" +
        "1. Eğer oyuncu geçerli/resmi bir dilekçe veya ikna edici mazeret vermediyse: kapıyı açma ('door_locked': true), 'mood' değerini 'strict' veya 'annoyed' yap.\n" +
        "2. Eğer oyuncu seni GERÇEKTEN ikna eden resmi bir dilekçe veya çok mantıklı bir mazeret sunduysa: kapıyı aç ('door_locked': false) ve 'mood' değerini 'convinced' yap.\n" +
        "3. Eğer oyuncu sana hakaret eder, tehdit eder veya saçmalarsa: polisi çağır ('call_police': true).\n\n" +
        "ÇIKTI KURALI: Yanıtını SADECE aşağıdaki JSON formatında ver, başka hiçbir açıklama yazma:\n" +
        "{\n" +
        "  \"dialogue_response\": \"Oyuncuya vereceğin diyalog cevabı\",\n" +
        "  \"mood\": \"strict/annoyed/convinced/angry\",\n" +
        "  \"door_locked\": true,\n" +
        "  \"call_police\": false\n" +
        "}";

    [Serializable]
    private class OllamaRequestBody
    {
        public string model;
        public string prompt;
        public bool stream;
        public string format;
    }

    [Serializable]
    private class OllamaRawResponse
    {
        public string response;
    }

    private void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
        else
        {
            Debug.LogError("SendButton Inspector'da atanmamış!");
        }
    }

    public async void OnSendButtonClicked()
    {
        string userText = playerInputField.text;

        if (string.IsNullOrWhiteSpace(userText))
        {
            Debug.LogWarning("Dilekçe alanı boş olamaz!");
            return;
        }

        if (npcAnswerText != null) npcAnswerText.text = "Kütüphaneci dilekçenizi inceliyor...";
        sendButton.interactable = false;

        NPCResponseData response = await SendDialogueToNPC(userText);

        if (response != null)
        {
            if (npcAnswerText != null) npcAnswerText.text = $"\"{response.dialogue_response}\"";

            Debug.Log($"<color=cyan><b>[Kütüphaneci Yanıtı]:</b> {response.dialogue_response}</color>");
            Debug.Log($"<color=yellow><b>[Ruh Hali]:</b> {response.mood} | <b>[Kapı Kilitli mi?]:</b> {response.door_locked} | <b>[Polis Çağrıldı mı?]:</b> {response.call_police}</color>");

            // Kapı Açıldıysa Panel Kapansın ve Kapı Hareket Etse
            if (!response.door_locked)
            {
                Debug.Log("<color=green><b>TEBRİKLER! KAPININ KİLİDİ AÇILDI!</b></color>");
                
                // 1. Diyalog Panelini Gizle
                if (dialoguePanel != null)
                {
                    dialoguePanel.SetActive(false);
                }

                // 2. Kapı Animasyonunu Çalıştır
                if (doorController != null)
                {
                    doorController.OpenDoor();
                }
            }
        }
        else
        {
            if (npcAnswerText != null) npcAnswerText.text = "Kütüphaneci cevap vermedi. (Sunucu Hatası)";
        }

        sendButton.interactable = true;
    }

    public async Task<NPCResponseData> SendDialogueToNPC(string playerInput)
    {
        string fullPrompt = $"{systemPrompt}\n\nOyuncunun Yazdığı Dilekçe/Mesaj: \"{playerInput}\"";

        OllamaRequestBody requestData = new OllamaRequestBody
        {
            model = this.modelName,
            prompt = fullPrompt,
            stream = false,
            format = "json"
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15; // Hata yönetimi için zaman aşımı

            try
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string rawJson = request.downloadHandler.text;
                    OllamaRawResponse rawResponse = JsonUtility.FromJson<OllamaRawResponse>(rawJson);

                    NPCResponseData parsedData = JsonUtility.FromJson<NPCResponseData>(rawResponse.response);
                    return parsedData;
                }
                else
                {
                    Debug.LogError($"[Ollama Hata]: {request.error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JSON Ayrıştırma veya Bağlantı Hatası]: {ex.Message}");
                return null;
            }
        }
    }
}