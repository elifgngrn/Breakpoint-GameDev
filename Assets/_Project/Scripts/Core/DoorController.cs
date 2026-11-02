using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Breakpoint.Core;

namespace Core
{
    public class DoorController : MonoBehaviour
    {
        private AudioSource audioSource;
        public AudioClip unlockSound; // Ses dosyasını buraya bağlayacağız

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void UnlockAndOpenDoor()
        {
            Debug.Log("Kapı mekanizması çalıştı, ses çalınıyor...");

            // Ses dosyası atanmışsa çal
            if (audioSource != null && unlockSound != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }

            // Doğrudan Build Index (1) numaralı Level_Corridor sahnesine geçiş yap
            SceneManager.LoadScene(1);
        }
    }
}