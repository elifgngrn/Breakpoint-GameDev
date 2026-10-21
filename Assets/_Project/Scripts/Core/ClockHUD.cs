using TMPro;
using UnityEngine;

public sealed class ClockHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && label != null)
            label.text = gm.Clock.Formatted;
    }
}
