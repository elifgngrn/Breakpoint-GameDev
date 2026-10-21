using UnityEngine;
using Core; // GameEvents'i tanıması için eklendi

public class LevelGecisi : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Koridor bitti, GameManager'a geçiş izni soruluyor...");
            GameEvents.RaiseCorridorExitReached(); // Sahneyi zorla açmak yerine sisteme haber veriyoruz
        }
    }
}