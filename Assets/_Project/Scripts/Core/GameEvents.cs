using System;

namespace Breakpoint.Core
{
    public static class GameEvents
    {
        // Diyalog Olayları (DiyalogUI açılıp kapanınca)
        public static event Action OnDialogueStarted;
        public static void RaiseDialogueStarted() => OnDialogueStarted?.Invoke();

        public static event Action OnDialogueEnded;
        public static void RaiseDialogueEnded() => OnDialogueEnded?.Invoke();

        // Kapı ve Kaçış Olayları
        public static event Action OnDoorUnlocked;
        public static void RaiseDoorUnlocked() => OnDoorUnlocked?.Invoke();

        public static event Action OnEscapeCompleted;
        public static void RaiseEscapeCompleted() => OnEscapeCompleted?.Invoke();
    }
}