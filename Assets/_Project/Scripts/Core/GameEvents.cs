using System;

namespace Core
{
    public static class GameEvents
    {
        // Zaman (GameClock) Olayları
        public static event Action TimeExpired;
        public static void RaiseTimeExpired() => TimeExpired?.Invoke();

        // Lazer ve Koridor Olayları
        public static event Action LasersDisabled;
        public static void RaiseLasersDisabled() => LasersDisabled?.Invoke();

        public static event Action CorridorExitReached;
        public static void RaiseCorridorExitReached() => CorridorExitReached?.Invoke();

        // Diyalog Olayları
        public static event Action DialogueStarted;
        public static void RaiseDialogueStarted() => DialogueStarted?.Invoke();

        public static event Action DialogueEnded;
        public static void RaiseDialogueEnded() => DialogueEnded?.Invoke();

        // Kapı ve Kaçış Olayları
        public static event Action DoorUnlocked;
        public static void RaiseDoorUnlocked() => DoorUnlocked?.Invoke();

        public static event Action EscapeCompleted;
        public static void RaiseEscapeCompleted() => EscapeCompleted?.Invoke();
    }
}