using TMPro;
using UnityEngine;

public sealed class ClockHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private void Update()
    {
        var gm = GameManager.Instance;
        if (label == null || gm == null || gm.Clock == null) return;
        label.text = gm.Clock.Formatted;
    }
}
