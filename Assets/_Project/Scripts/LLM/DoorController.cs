using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private bool isLocked = true;
    [SerializeField] private float openDistance = 5f; // Kapının yukarı kayma miktarı

    public void UnlockDoor()
    {
        if (!isLocked) return; // Zaten açıldıysa tekrar çalışma

        isLocked = false;
        Debug.Log("<color=green><b>[KAPI AÇILDI]:</b> Kütüphaneci ikna oldu, kapı fiziksel olarak açılıyor!</color>");
        
        // Kapı objesini yukarı doğru kaydırır
        transform.position += new Vector3(0, openDistance, 0);
    }

    public bool IsLocked() => isLocked;
}