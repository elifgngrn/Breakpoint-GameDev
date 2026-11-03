using System;

[Serializable]
public class NPCResponseData
{
    public string dialogue; // Kütüphanecinin oyuncuya verdiği yanıt
    public bool door_locked;
    public bool call_police;
    public bool usedFallback; // Ollama yanıtı alınamadığında yerel metin kullanıldı
}
