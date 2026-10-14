using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private bool isLocked = true;

    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("<color=green><b>[KAPI AÇILDI]:</b> Kütüphaneci ikna oldu, çıkış kapısı açıldı!</color>");

        // Kapıyı görsel olarak yukarı kaydırarak açar
        transform.Translate(Vector3.up * 3f);
    }

    public bool IsLocked() => isLocked;
}