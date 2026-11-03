using UnityEngine;
using Breakpoint.Core;

[RequireComponent(typeof(Collider))]
public sealed class StairDescentTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.StoryStage != LibraryStoryStage.DescendStairs) return;

        manager.SetStoryStage(LibraryStoryStage.ClearDebris);
        GameEvents.RaiseNotification("Alt kat: devrilmiş rafları robotun yardımıyla temizle.");
    }
}
