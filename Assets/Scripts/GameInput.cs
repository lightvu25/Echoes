using System;
using UnityEngine;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler OnMenuButtonPressed;

    private InputActions inputActions;

    private void Awake()
    {
        Instance = this;

        inputActions = new InputActions();
        inputActions.Enable();

        inputActions.Player.Menu.performed += Menu_performed;
    }

    private void Menu_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnMenuButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy()
    {
        inputActions.Disable();
    }

    public bool IsUpActionPressed()
    {
        return inputActions.Player.PlayerUp.IsPressed();
    }

    public bool IsDownActionPressed()
    {
        return inputActions.Player.PlayerDown.IsPressed();
    }

    public bool IsLeftActionPressed()
    {
        return inputActions.Player.PlayerLeft.IsPressed();
    }

    public bool IsRightActionPressed()
    {
        return inputActions.Player.PlayerRight.IsPressed();
    }

    public bool IsJumpActionPressed()
    {
        return inputActions.Player.PlayerJump.WasPressedThisFrame();
    }

    public bool IsDashActionPressed()
    {
        return inputActions.Player.PlayerDash.IsPressed();
    }

    public bool IsPauseActionPressed()
    {
        return inputActions.Player.Menu.IsPressed();
    }
}
