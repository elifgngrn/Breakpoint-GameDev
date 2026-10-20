using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Kapı Ayarları")]
    [SerializeField] private Vector3 openOffset = new Vector3(0, 3f, 0); // Yükseklik miktarı
    [SerializeField] private float speed = 2f;                         // Açılma hızı

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen = false;

    private void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
    }

    public void OpenDoor()
    {
        if (!isOpen)
        {
            isOpen = true;
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
        Debug.Log("<color=green>Kapı başarıyla açıldı!</color>");
    }
}