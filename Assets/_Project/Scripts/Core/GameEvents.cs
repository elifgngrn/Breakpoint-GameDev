using System;

namespace Breakpoint.Core
{
    public static class GameEvents
    {
        // --- EVENT TANIMLARI ---
        public static Action LasersDisabled;
        public static Action CorridorExitReached;
        public static Action DialogueStarted;
        public static Action DialogueEnded;
        public static Action DoorUnlocked;
        public static Action EscapeCompleted;
        public static Action TimeExpired;

        // --- TETİKLEYİCİ METOTLAR (Raise Methods) ---
        public static void RaiseLasersDisabled() => LasersDisabled?.Invoke();
        public static void RaiseCorridorExitReached() => CorridorExitReached?.Invoke();
        public static void RaiseDialogueStarted() => DialogueStarted?.Invoke();
        public static void RaiseDialogueEnded() => DialogueEnded?.Invoke();
        public static void RaiseDoorUnlocked() => DoorUnlocked?.Invoke();
        public static void RaiseEscapeCompleted() => EscapeCompleted?.Invoke();
        public static void RaiseTimeExpired() => TimeExpired?.Invoke();
    }
}