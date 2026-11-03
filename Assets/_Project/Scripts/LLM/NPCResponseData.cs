using System;

[Serializable]
public class NPCResponseData
{
    public string dialogue;
    public string doorDecision; // Modelden gelen "unlock" veya "keep_locked" eylemi
    public bool door_locked;
    public bool call_police;
    public bool usedFallback; // Model yanıtı alınamadı veya geçersizdi
}
