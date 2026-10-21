using UnityEngine;

/// <summary>Koridor sonuna konan trigger collider. Oyuncu "Player" tag'li olmalı.</summary>
[RequireComponent(typeof(Collider))]
public sealed class CorridorExitTrigger : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            GameEvents.RaiseCorridorExitReached();
    }
}
