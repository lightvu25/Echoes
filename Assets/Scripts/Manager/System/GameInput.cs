using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler OnMenuButtonPressed;

    public event Action<int> OnHotbarKeyPressed;
    public event Action OnCycleNextPressed;
    public event Action OnCyclePrevPressed;

    public event Action OnMapTogglePressed;
    public event Action OnInventoryPressed;
    public event Action OnInteractPressed;
    public event Action OnExtractPressed;
    public event Action OnCancelPressed;
    public event Action OnHealPressed;
    public event Action OnToolPressed;

    [Header("Input Configuration")]
    [SerializeField] private InputConfig inputConfig;

    private InputActions inputActions;
    private InputConfig.ControlScheme lastScheme;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        inputActions = new InputActions();
        inputActions.Enable();

        inputActions.Player.Menu.performed += Menu_performed;
    }

    private void Start()
    {
        if (inputConfig != null)
        {
            lastScheme = inputConfig.activeScheme;
            ApplyBindings();
        }
    }

    private void Update()
    {
        if (inputConfig != null && inputConfig.activeScheme != lastScheme)
        {
            lastScheme = inputConfig.activeScheme;
            ApplyBindings();
        }

        // Hotbar & Cycle Inputs
        if (Input.GetKeyDown(KeyCode.Alpha1)) OnHotbarKeyPressed?.Invoke(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) OnHotbarKeyPressed?.Invoke(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) OnHotbarKeyPressed?.Invoke(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) OnHotbarKeyPressed?.Invoke(3);
        
        if (Input.GetKeyDown(KeyCode.Q)) OnCyclePrevPressed?.Invoke();
        
        KeyCode mapKey = inputConfig != null ? inputConfig.mapKey : KeyCode.M;
        KeyCode invKey = inputConfig != null ? inputConfig.inventoryKey : KeyCode.E;
        KeyCode interactKey = inputConfig != null ? inputConfig.interactKey : KeyCode.F;
        KeyCode extractKey = inputConfig != null ? inputConfig.extractKey : KeyCode.R;
        KeyCode cancelKey = inputConfig != null ? inputConfig.cancelKey : KeyCode.Escape;
        KeyCode healKey = inputConfig != null ? inputConfig.healKey : KeyCode.H;
        KeyCode toolKey = inputConfig != null ? inputConfig.toolKey : KeyCode.T;

        if (Input.GetKeyDown(mapKey)) OnMapTogglePressed?.Invoke();
        if (Input.GetKeyDown(invKey)) OnInventoryPressed?.Invoke();

        if (Input.GetKeyDown(interactKey)) OnInteractPressed?.Invoke();
        if (Input.GetKeyDown(extractKey)) OnExtractPressed?.Invoke();
        if (Input.GetKeyDown(cancelKey)) OnCancelPressed?.Invoke();
        if (Input.GetKeyDown(healKey)) OnHealPressed?.Invoke();
        if (Input.GetKeyDown(toolKey)) OnToolPressed?.Invoke();
    }

    private void ApplyBindings()
    {
        if (inputConfig == null) return;

        bool isWASD = inputConfig.activeScheme == InputConfig.ControlScheme.WASD_JUK;

        ApplyBinding(inputActions.Player.PlayerLeft, isWASD ? "<Keyboard>/a" : "<Keyboard>/leftArrow");
        ApplyBinding(inputActions.Player.PlayerRight, isWASD ? "<Keyboard>/d" : "<Keyboard>/rightArrow");
        ApplyBinding(inputActions.Player.PlayerUp, isWASD ? "<Keyboard>/w" : "<Keyboard>/upArrow");
        ApplyBinding(inputActions.Player.PlayerDown, isWASD ? "<Keyboard>/s" : "<Keyboard>/downArrow");

        ApplyBinding(inputActions.Player.Attack, GetInputPath(inputConfig.AttackKey));
        ApplyBinding(inputActions.Player.Skill, GetInputPath(inputConfig.SkillKey));
        ApplyBinding(inputActions.Player.Special, GetInputPath(inputConfig.SpecialKey));
        ApplyBinding(inputActions.Player.PlayerDash, GetInputPath(inputConfig.DashKey));
        ApplyBinding(inputActions.Player.PlayerJump, GetInputPath(inputConfig.JumpKey));
        
    }

    private void ApplyBinding(InputAction action, string path)
    {
        action.ApplyBindingOverride(new InputBinding { overridePath = path });
    }

    private string GetInputPath(KeyCode key)
    {
        return key switch
        {
            KeyCode.A => "<Keyboard>/a", KeyCode.B => "<Keyboard>/b", KeyCode.C => "<Keyboard>/c",
            KeyCode.D => "<Keyboard>/d", KeyCode.E => "<Keyboard>/e", KeyCode.F => "<Keyboard>/f",
            KeyCode.G => "<Keyboard>/g", KeyCode.H => "<Keyboard>/h", KeyCode.I => "<Keyboard>/i",
            KeyCode.J => "<Keyboard>/j", KeyCode.K => "<Keyboard>/k", KeyCode.L => "<Keyboard>/l",
            KeyCode.M => "<Keyboard>/m", KeyCode.N => "<Keyboard>/n", KeyCode.O => "<Keyboard>/o",
            KeyCode.P => "<Keyboard>/p", KeyCode.Q => "<Keyboard>/q", KeyCode.R => "<Keyboard>/r",
            KeyCode.S => "<Keyboard>/s", KeyCode.T => "<Keyboard>/t", KeyCode.U => "<Keyboard>/u",
            KeyCode.V => "<Keyboard>/v", KeyCode.W => "<Keyboard>/w", KeyCode.X => "<Keyboard>/x",
            KeyCode.Y => "<Keyboard>/y", KeyCode.Z => "<Keyboard>/z",
            
            KeyCode.UpArrow => "<Keyboard>/upArrow", KeyCode.DownArrow => "<Keyboard>/downArrow",
            KeyCode.LeftArrow => "<Keyboard>/leftArrow", KeyCode.RightArrow => "<Keyboard>/rightArrow",
            
            KeyCode.LeftShift => "<Keyboard>/leftShift", KeyCode.RightShift => "<Keyboard>/rightShift",
            KeyCode.LeftControl => "<Keyboard>/leftCtrl", KeyCode.RightControl => "<Keyboard>/rightCtrl",
            KeyCode.LeftAlt => "<Keyboard>/leftAlt", KeyCode.RightAlt => "<Keyboard>/rightAlt",
            
            KeyCode.Space => "<Keyboard>/space", KeyCode.Return => "<Keyboard>/enter",
            KeyCode.Escape => "<Keyboard>/escape", KeyCode.Tab => "<Keyboard>/tab",
            KeyCode.Backspace => "<Keyboard>/backspace",
            _ => "<Keyboard>/space" 
        };
    }

    private void Menu_performed(InputAction.CallbackContext obj) => OnMenuButtonPressed?.Invoke(this, EventArgs.Empty);
    private void OnDestroy() 
    {
        if (inputActions != null)
        {
            inputActions.Player.Menu.performed -= Menu_performed;
            inputActions.Disable();
        }
    }

    public void SetInputsEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            inputActions?.Enable();
            this.enabled = true;
        }
        else
        {
            inputActions?.Disable();
            this.enabled = false;
        }
    }

    public bool IsUpActionPressed() => inputActions.Player.PlayerUp.IsPressed();
    public bool IsDownActionPressed() => inputActions.Player.PlayerDown.IsPressed();
    public bool IsLeftActionPressed() => inputActions.Player.PlayerLeft.IsPressed();
    public bool IsRightActionPressed() => inputActions.Player.PlayerRight.IsPressed();
    
    public bool IsJumpActionPressed() => inputActions.Player.PlayerJump.WasPressedThisFrame();
    public bool IsDashActionPressed() => inputActions.Player.PlayerDash.WasPressedThisFrame();
    public bool IsAttackActionPressed() => inputActions.Player.Attack.WasPressedThisFrame();
    public bool IsSkillActionPressed() => inputActions.Player.Skill.WasPressedThisFrame();
    public bool IsSpecialActionPressed() => inputActions.Player.Special.WasPressedThisFrame();
    
    public bool IsJumpActionHeld() => inputActions.Player.PlayerJump.IsPressed();
    public bool IsJumpActionReleased() => inputActions.Player.PlayerJump.WasReleasedThisFrame();
    
    public bool IsAttackActionHeld() => inputActions.Player.Attack.IsPressed();
    public bool IsAttackActionReleased() => inputActions.Player.Attack.WasReleasedThisFrame();
    
    public bool IsPauseActionPressed() => inputActions.Player.Menu.IsPressed();
}