using UnityEngine;
using Breakpoint.Core;

namespace Core
{
    /// <summary>
    /// Kapıyı yalnızca diyalog kararıyla açar. Oyuncu açılmış kapının trigger alanından
    /// geçtiğinde kaçış tamamlanır; kapıyı açmak tek başına sahne/oyun geçişi başlatmaz.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private AudioClip unlockSound;

        private AudioSource audioSource;
        private Collider doorCollider;
        private bool isUnlocked;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            doorCollider = GetComponent<Collider>();

            if (doorCollider == null)
            {
                Debug.LogError("DoorController: Kapı Collider bileşeni bulunamadı.", this);
                enabled = false;
                return;
            }

            // Kapı kilitliyken fiziksel engel, açılınca geçiş trigger'ı olarak çalışır.
            doorCollider.isTrigger = false;
        }

        public void UnlockAndOpenDoor()
        {
            if (!enabled || doorCollider == null || isUnlocked)
                return;

            isUnlocked = true;
            doorCollider.isTrigger = true;

            if (audioSource != null && unlockSound != null)
                audioSource.PlayOneShot(unlockSound);

            Debug.Log("Kapı açıldı. Kaçışı tamamlamak için kapı trigger alanından geçin.", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isUnlocked || other == null || !other.CompareTag("Player"))
                return;

            GameEvents.RaiseEscapeCompleted();
        }
    }
}
