using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGecisi : MonoBehaviour
{
    // Başka bir obje bu görünmez alana (Trigger) girdiğinde otomatik çalışır
    private void OnTriggerEnter(Collider other)
    {
        // Eğer alana giren objenin etiketi "Player" ise (Kameramız/Karakterimiz)
        if (other.CompareTag("Player"))
        {
            Debug.Log("Koridor bitti, diğer sahneye geçiliyor...");
            SceneManager.LoadScene("Level_Door"); // Arkadaşının sahnesinin tam adı
        }
    }
}
