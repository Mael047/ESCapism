using UnityEngine;
using UnityEngine.InputSystem;

public static class MenuPause
{
    public static bool IsPaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => IsPaused = false;

    public static void Enter()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        InputSystem.actions.FindActionMap("Player")?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void Exit()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        InputSystem.actions.FindActionMap("Player")?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}