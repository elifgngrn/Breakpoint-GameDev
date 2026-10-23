using UnityEngine;
using TMPro;
using Core;

public class DialogueUI : MonoBehaviour
{
    [Header("Arayüz Bağlantıları")]
    public GameObject dialoguePanel;
    public TMP_Text npcAnswerText;
    
    // Bu fonksiyon robot kapıya ulaşıp diyaloğu tetiklediğinde çalışacak
    public void DiyaloguBaslat()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        
        GameEvents.RaiseDialogueStarted(); // Oyuncunun kamerasını ve hareketini dondurur
        Debug.Log("Diyalog paneli açıldı, oyuncu donduruldu.");
    }

    // Bu fonksiyon diyalog bittiğinde çalışacak
    public void DiyaloguBitir()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        
        GameEvents.RaiseDialogueEnded(); // Oyuncuyu tekrar serbest bırakır
        Debug.Log("Diyalog paneli kapandı, oyuncu serbest.");
    }
}