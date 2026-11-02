using System;

[Serializable]
public class NPCResponseData
{
    public string dialogue; // Kütüphanecinin oyuncuya verdiği yanıt
    public bool usedFallback; // Ollama yanıtı alınamadığında yerel metin kullanıldı
}
