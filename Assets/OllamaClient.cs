using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaClient : MonoBehaviour
{
    [SerializeField] private string ollamaUrl = "http://localhost:11434/api/generate";
    [SerializeField] private string modelName = "llama3.2";

    private readonly string systemPrompt = 
        "Sen 'Vize Haftası Kütüphane Firarı' oyunundaki nöbetçi kütüphanecisin. " +
        "Saat gece 04:00. Aşırı huysuzsun, bürokrasiden ve kurallardan asla taviz vermezsin. " +
        "Çıkış kapısını kilitli tutuyorsun. Oyuncunun dışarı çıkması için geçerli bir dilekçe veya resmi mazeret sunması gerekir. " +
        "SADECE ve SADECE aşağıdaki JSON formatında yanıt vermelisin. Başka hiçbir açıklama veya metin yazma!\n\n" +
        "JSON Şeması:\n" +
        "{\n" +
        "  \"dialogue_response\": \"(NPC'nin oyuncuya söyleyeceği Türkçe cümle)\",\n" +
        "  \"mood\": \"(annoyed, suspicious, convinced, furious değerlerinden biri)\",\n" +
        "  \"door_locked\": (true veya false),\n" +
        "  \"call_police\": (true veya false)\n" +
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

    private async void Start()
    {
        Debug.Log("Nöbetçi Kütüphaneci ile diyalog başlatılıyor...");

        string playerMessage = "İyi geceler memur bey, vize sınavım yarın sabah 8'de. Lütfen kapıyı açın, eve gidip uyumam lazım!";

        NPCResponseData npcAnswer = await SendDialogueToNPC(playerMessage);

        if (npcAnswer != null)
        {
            Debug.Log($"<color=cyan><b>[Kütüphaneci Yanıtı]:</b> {npcAnswer.dialogue_response}</color>");
            Debug.Log($"<color=yellow><b>[Ruh Hali]:</b> {npcAnswer.mood} | <b>[Kapı Kilitli mi?]:</b> {npcAnswer.door_locked} | <b>[Polis Çağrıldı mı?]:</b> {npcAnswer.call_police}</color>");
        }
        else
        {
            Debug.LogError("<color=red>Kütüphaneciden geçerli bir JSON yanıtı alınamadı!</color>");
        }
    }

    public async Task<NPCResponseData> SendDialogueToNPC(string playerInput)
    {
        string fullPrompt = $"{systemPrompt}\n\nOyuncunun Söylediği: \"{playerInput}\"";

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