using UnityEngine;

public interface ICursorController
{
    void SetGameplayMode();
    void SetUIMode();
}

public sealed class CursorController : ICursorController
{
    public void SetGameplayMode()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetUIMode()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
