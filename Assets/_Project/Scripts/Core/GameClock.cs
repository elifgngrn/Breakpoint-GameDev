using UnityEngine;
using Breakpoint.Core;
using Core;


/// <summary>04:00 → 09:00 oyun içi saati, gerçek süreye (varsayılan 10 dk) eşler.</summary>
public sealed class GameClock : MonoBehaviour
{
    [SerializeField] private float realDurationSeconds = 600f;
    [SerializeField] private float startHour = 4f;
    [SerializeField] private float endHour = 9f;

    private float elapsed;
    private bool expiredRaised;

    public bool Running { get; private set; }
    public float Normalized => Mathf.Clamp01(elapsed / realDurationSeconds);
    public float CurrentHour => Mathf.Lerp(startHour, endHour, Normalized);

    public string Formatted
    {
        get
        {
            int totalMinutes = Mathf.FloorToInt(CurrentHour * 60f);
            return $"{totalMinutes / 60:00}:{totalMinutes % 60:00}";
        }
    }

    public void StartClock() => Running = true;
    public void StopClock()  => Running = false;

    public void ResetClock()
    {
        elapsed = 0f;
        expiredRaised = false;
        Running = false;
    }

    private void Update()
    {
        if (!Running) return;

        elapsed += Time.deltaTime;
        if (elapsed >= realDurationSeconds && !expiredRaised)
        {
            expiredRaised = true;
            Running = false;
            GameEvents.RaiseTimeExpired();
        }
    }
}
