using System.Collections;
using UnityEngine;
using Breakpoint.Core;
namespace Core
{
    using UnityEngine;

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

            // Burada kapının açılma animasyonu veya pozisyon değişimi yer alıyor olabilir
        }
    }
}