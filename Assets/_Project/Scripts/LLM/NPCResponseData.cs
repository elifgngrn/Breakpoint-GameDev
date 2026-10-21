using System;

[Serializable]
public class NPCResponseData
{
    public string dialogue;     // Kütüphanecinin yanıtı
    public bool door_locked;    // Kapının kilitli olma durumu
    public bool call_police;    // Polisin çağrılıp çağrılmadığı
}