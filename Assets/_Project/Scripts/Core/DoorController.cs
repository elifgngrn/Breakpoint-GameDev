using System.Collections;
using UnityEngine;

namespace Core
{
    public class DoorController : MonoBehaviour
    {
        [Header("Kapı Fiziksel Hareket Ayarları")]
        [SerializeField] private Vector3 openOffset = new Vector3(0, 3f, 0);
        [SerializeField] private float speed = 2f;

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isOpen = false;

        private void Start()
        {
            closedPosition = transform.position;
            openPosition = closedPosition + openOffset;
        }

        public void UnlockAndOpenDoor()
        {
            if (!isOpen)
            {
                isOpen = true;
                GameEvents.RaiseDoorUnlocked(); // Core Event tetiklendi[cite: 5]
                Debug.Log("<color=green>Çıkış kapısının kilidi açıldı ve hareket başlatıldı!</color>");

                StartCoroutine(AnimateDoorOpen());
            }
        }

        private IEnumerator AnimateDoorOpen()
        {
            float time = 0;
            while (time < 1)
            {
                transform.position = Vector3.Lerp(closedPosition, openPosition, time);
                time += Time.deltaTime * speed;
                yield return null;
            }
            transform.position = openPosition;
            
            CompleteEscapeSequence();
        }

        public void CompleteEscapeSequence()
        {
            GameEvents.RaiseEscapeCompleted(); // Kaçış tamamlandı event'i[cite: 5]
            Debug.Log("<color=cyan>Kütüphaneden başarıyla firar edildi!</color>");
        }
    }
}