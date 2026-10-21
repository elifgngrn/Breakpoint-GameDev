using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Core;

public class OllamaManager : MonoBehaviour
{
    [SerializeField] private string ollamaUrl = "http://localhost:11434/api/generate";
    [SerializeField] private string modelName = "llama3.2";

    public async Task<NPCResponseData> SendPromptToOllama(string userMessage)
    {
        string systemPrompt = "Sen Kocaeli Üniversitesi kütüphanesinde gece 04:00 nöbetinde olan, bürokrasiden ve kurallardan asla taviz vermeyen aksi bir güvenlik görevlisisin. Oyuncu vize haftasında kütüphaneden kaçmak istiyor. Geçerli bir mazeret veya imzalı dilekçe sunulmadığı sürece kapıyı açma. YANITINI SADECE AŞAĞIDAKİ JSON FORMATINDA VER:\\n{\\\"dialogue\\\": \\\"yanıtın\\\", \\\"door_locked\\\": true/false, \\\"call_police\\\": true/false}";

        string jsonRequestBody = $"{{\"model\": \"{modelName}\", \"prompt\": \"{userMessage}\", \"system\": \"{systemPrompt}\", \"stream\": false}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 8; // 8 saniye Timeout (Zaman aşımı koruması)

            try
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string rawResponse = request.downloadHandler.text;
                    return ParseOllamaResponse(rawResponse);
                }
                else
                {
                    Debug.LogWarning("Ollama Bağlantı Hatası: " + request.error);
                    return GetFallbackResponse("Nöbetçi kütüphaneci şu an uyuyor (Ollama sunucusuna ulaşılamadı).");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Ollama İstisnası: " + ex.Message);
                return GetFallbackResponse("Bir sistem hatası oluştu.");
            }
        }
    }

    private NPCResponseData ParseOllamaResponse(string jsonRaw)
    {
        try
        {
            // Model çıktısını ayrıştırma
            NPCResponseData data = JsonUtility.FromJson<NPCResponseData>(jsonRaw);
            if (data != null && !string.IsNullOrEmpty(data.dialogue))
            {
                return data;
            }
            return GetFallbackResponse("Kütüphaneci mırıldandı ama ne dediği anlaşılmadı.");
        }
        catch (Exception)
        {
            // Bozuk JSON koruması
            return GetFallbackResponse("Düzgün bir dilekçe yazmadın evladım, anlayamadım!");
        }
    }

    private NPCResponseData GetFallbackResponse(string defaultDialogue)
    {
        return new NPCResponseData
        {
            dialogue = defaultDialogue,
            door_locked = true,
            call_police = false
        };
    }
}