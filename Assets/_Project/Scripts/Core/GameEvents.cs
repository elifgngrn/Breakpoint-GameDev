using System;

/// <summary>
/// Robot tarafı (A) ile LLM/arayüz tarafı (B) arasındaki tek temas noktası.
/// Hiçbir taraf diğerinin sınıfını bilmez; sadece buradaki olayları yayınlar/dinler.
/// </summary>
public static class GameEvents
{
    // --- A tarafı (robot & fizik) yayınlar ---
    public static event Action LasersDisabled;
    public static event Action CorridorExitReached;

    // --- B tarafı (LLM & arayüz) yayınlar ---
    public static event Action DialogueStarted;
    public static event Action DialogueEnded;
    public static event Action DoorUnlocked;     // LLM "kapiyi_ac" döndürünce
    public static event Action EscapeCompleted;  // kaçış sekansı bitince

    // --- GameManager/GameClock yayınlar ---
    public static event Action TimeExpired;

    public static void RaiseLasersDisabled()      => LasersDisabled?.Invoke();
    public static void RaiseCorridorExitReached() => CorridorExitReached?.Invoke();
    public static void RaiseDialogueStarted()     => DialogueStarted?.Invoke();
    public static void RaiseDialogueEnded()       => DialogueEnded?.Invoke();
    public static void RaiseDoorUnlocked()        => DoorUnlocked?.Invoke();
    public static void RaiseEscapeCompleted()     => EscapeCompleted?.Invoke();
    public static void RaiseTimeExpired()         => TimeExpired?.Invoke();
}
