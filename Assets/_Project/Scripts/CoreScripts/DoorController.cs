using System.Collections;
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
        [SerializeField, Min(0.1f)] private float openDuration = 0.7f;
        [SerializeField, Min(0.1f)] private float openDistance = 3.2f;

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

            StartCoroutine(SlideDoorOpen());
            Debug.Log("Kapı açıldı. Kaçışı tamamlamak için çıkıştan geçin.", this);
        }

        private IEnumerator SlideDoorOpen()
        {
            Vector3 start = transform.localPosition;
            Vector3 end = start + Vector3.right * openDistance;
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                float eased = t * t * (3f - 2f * t);
                transform.localPosition = Vector3.LerpUnclamped(start, end, eased);
                yield return null;
            }

            transform.localPosition = end;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isUnlocked || other == null || !other.CompareTag("Player"))
                return;

            GameEvents.RaiseEscapeCompleted();
        }
    }
}
