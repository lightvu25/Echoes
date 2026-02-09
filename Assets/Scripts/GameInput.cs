using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler OnMenuButtonPressed;

    [Header("Input Configuration")]
    [SerializeField] private InputConfig inputConfig;

    private InputActions inputActions;
    private InputConfig.ControlScheme lastScheme;

    private void Awake()
    {
        Instance = this;

        inputActions = new InputActions();
        inputActions.Enable();

        inputActions.Player.Menu.performed += Menu_performed;
    }

    private void Start()
    {
        // Apply initial bindings based on InputConfig
        if (inputConfig != null)
        {
            lastScheme = inputConfig.activeScheme;
            ApplyBindings();
        }
    }

    private void Update()
    {
        // Check if scheme changed at runtime
        if (inputConfig != null && inputConfig.activeScheme != lastScheme)
        {
            lastScheme = inputConfig.activeScheme;
            ApplyBindings();
        }
    }

    private void ApplyBindings()
    {
        if (inputConfig == null) return;

        bool isWASD = inputConfig.activeScheme == InputConfig.ControlScheme.WASD_JUK;

        // Movement bindings
        ApplyBinding(inputActions.Player.PlayerLeft, isWASD ? "<Keyboard>/a" : "<Keyboard>/leftArrow");
        ApplyBinding(inputActions.Player.PlayerRight, isWASD ? "<Keyboard>/d" : "<Keyboard>/rightArrow");
        ApplyBinding(inputActions.Player.PlayerUp, isWASD ? "<Keyboard>/w" : "<Keyboard>/upArrow");
        ApplyBinding(inputActions.Player.PlayerDown, isWASD ? "<Keyboard>/s" : "<Keyboard>/downArrow");

        // Combat bindings
        ApplyBinding(inputActions.Player.Attack, GetInputPath(inputConfig.AttackKey));
        ApplyBinding(inputActions.Player.Skill, GetInputPath(inputConfig.SkillKey));
        ApplyBinding(inputActions.Player.Special, GetInputPath(inputConfig.SpecialKey));
        ApplyBinding(inputActions.Player.PlayerDash, GetInputPath(inputConfig.DashKey));
        ApplyBinding(inputActions.Player.PlayerJump, GetInputPath(inputConfig.JumpKey));

        Debug.Log($"[GameInput] Applied bindings for scheme: {inputConfig.activeScheme}");
    }

    private void ApplyBinding(InputAction action, string path)
    {
        // Remove all existing bindings and apply new one
        action.ApplyBindingOverride(new InputBinding { overridePath = path });
    }

    private string GetInputPath(KeyCode key)
    {
        // Convert Unity KeyCode to Input System path
        return key switch
        {
            // Letters
            KeyCode.A => "<Keyboard>/a",
            KeyCode.B => "<Keyboard>/b",
            KeyCode.C => "<Keyboard>/c",
            KeyCode.D => "<Keyboard>/d",
            KeyCode.E => "<Keyboard>/e",
            KeyCode.F => "<Keyboard>/f",
            KeyCode.G => "<Keyboard>/g",
            KeyCode.H => "<Keyboard>/h",
            KeyCode.I => "<Keyboard>/i",
            KeyCode.J => "<Keyboard>/j",
            KeyCode.K => "<Keyboard>/k",
            KeyCode.L => "<Keyboard>/l",
            KeyCode.M => "<Keyboard>/m",
            KeyCode.N => "<Keyboard>/n",
            KeyCode.O => "<Keyboard>/o",
            KeyCode.P => "<Keyboard>/p",
            KeyCode.Q => "<Keyboard>/q",
            KeyCode.R => "<Keyboard>/r",
            KeyCode.S => "<Keyboard>/s",
            KeyCode.T => "<Keyboard>/t",
            KeyCode.U => "<Keyboard>/u",
            KeyCode.V => "<Keyboard>/v",
            KeyCode.W => "<Keyboard>/w",
            KeyCode.X => "<Keyboard>/x",
            KeyCode.Y => "<Keyboard>/y",
            KeyCode.Z => "<Keyboard>/z",
            // Arrows
            KeyCode.UpArrow => "<Keyboard>/upArrow",
            KeyCode.DownArrow => "<Keyboard>/downArrow",
            KeyCode.LeftArrow => "<Keyboard>/leftArrow",
            KeyCode.RightArrow => "<Keyboard>/rightArrow",
            // Modifiers
            KeyCode.LeftShift => "<Keyboard>/leftShift",
            KeyCode.RightShift => "<Keyboard>/rightShift",
            KeyCode.LeftControl => "<Keyboard>/leftCtrl",
            KeyCode.RightControl => "<Keyboard>/rightCtrl",
            KeyCode.LeftAlt => "<Keyboard>/leftAlt",
            KeyCode.RightAlt => "<Keyboard>/rightAlt",
            // Special
            KeyCode.Space => "<Keyboard>/space",
            KeyCode.Return => "<Keyboard>/enter",
            KeyCode.Escape => "<Keyboard>/escape",
            KeyCode.Tab => "<Keyboard>/tab",
            KeyCode.Backspace => "<Keyboard>/backspace",
            _ => "<Keyboard>/space" // Fallback
        };
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
        return inputActions.Player.PlayerDash.WasPressedThisFrame();
    }

    public bool IsAttackActionPressed()
    {
        return inputActions.Player.Attack.WasPressedThisFrame();
    }

    public bool IsSkillActionPressed()
    {
        return inputActions.Player.Skill.WasPressedThisFrame();
    }

    public bool IsSpecialActionPressed()
    {
        return inputActions.Player.Special.WasPressedThisFrame();
    }

    public bool IsJumpActionHeld()
    {
        return inputActions.Player.PlayerJump.IsPressed();
    }
    public bool IsJumpActionReleased()
    {
        return inputActions.Player.PlayerJump.WasReleasedThisFrame();
    }

    public bool IsAttackActionHeld()
    {
        return inputActions.Player.Attack.IsPressed();
    }
    public bool IsAttackActionReleased()
    {
        return inputActions.Player.Attack.WasReleasedThisFrame();
    }

    public bool IsPauseActionPressed()
    {
        return inputActions.Player.Menu.IsPressed();
    }
}
