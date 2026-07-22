using UnityEngine;

[CreateAssetMenu(menuName = "Data/Input Config")]
public class InputConfig : ScriptableObject
{
    public enum ControlScheme
    {
        WASD_JUK,    // Set 1: WASD movement, J/U/K combat, L dash
        Arrow_ZXC    // Set 2: Arrow movement, Z/X/C combat, L-Shift dash
    }

    [Header("Active Scheme")]
    public ControlScheme activeScheme = ControlScheme.WASD_JUK;

    // Combat Keys (Kept for Inspector configuration/UI display, but logic uses New Input System Bindings)  
    [Header("Scheme 1: WASD + JUK (Combat)")]
    public KeyCode wasd_Attack = KeyCode.J;
    public KeyCode wasd_Skill = KeyCode.U;
    public KeyCode wasd_Special = KeyCode.K;
    public KeyCode wasd_Dash = KeyCode.LeftShift;
    public KeyCode wasd_Jump = KeyCode.W;

    [Header("Scheme 2: Arrow + ZXC (Combat)")]
    public KeyCode arrow_Attack = KeyCode.Z;
    public KeyCode arrow_Skill = KeyCode.X;
    public KeyCode arrow_Special = KeyCode.C;
    public KeyCode arrow_Dash = KeyCode.LeftShift;
    public KeyCode arrow_Jump = KeyCode.UpArrow;

    // Combat Input Properties (Wrappers for UI/Display)  
    public KeyCode AttackKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Attack : arrow_Attack;
    public KeyCode SkillKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Skill : arrow_Skill;
    public KeyCode SpecialKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Special : arrow_Special;
    public KeyCode DashKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Dash : arrow_Dash;
    public KeyCode JumpKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Jump : arrow_Jump;

    [Header("General Input")]
    public KeyCode interactKey = KeyCode.F;
    public KeyCode extractKey = KeyCode.R;
    public KeyCode inventoryKey = KeyCode.E;
    public KeyCode mapKey = KeyCode.M;
    public KeyCode cancelKey = KeyCode.Escape;
    public KeyCode healKey = KeyCode.H;
    public KeyCode toolKey = KeyCode.T;

    [Header("Playstyle Keys")]
    public KeyCode meleeKey = KeyCode.J;
    public KeyCode midRangeKey = KeyCode.K;
    public KeyCode longRangeKey = KeyCode.L;
    public KeyCode magicKey = KeyCode.Semicolon;

    // Combat Input Methods
    public bool GetAttackDown() => GameInput.Instance != null && GameInput.Instance.IsAttackActionPressed();
    public bool GetAttackHeld() => GameInput.Instance != null && GameInput.Instance.IsAttackActionHeld();
    public bool GetAttackUp() => GameInput.Instance != null && GameInput.Instance.IsAttackActionReleased();
    
    public bool GetSkillDown() => GameInput.Instance != null && GameInput.Instance.IsSkillActionPressed();
    public bool GetSpecialDown() => GameInput.Instance != null && GameInput.Instance.IsSpecialActionPressed();

    // Playstyle Inputs
    public bool GetAttackMeleeDown() => Input.GetKeyDown(meleeKey) || GetAttackDown(); // Fallback to primary attack
    public bool GetAttackMeleeHeld() => Input.GetKey(meleeKey) || GetAttackHeld();
    
    public bool GetAttackMidDown() => Input.GetKeyDown(midRangeKey);
    public bool GetAttackMidHeld() => Input.GetKey(midRangeKey);
    
    public bool GetAttackLongDown() => Input.GetKeyDown(longRangeKey);
    public bool GetAttackLongHeld() => Input.GetKey(longRangeKey);
    
    public bool GetAttackMagicDown() => Input.GetKeyDown(magicKey);
    public bool GetAttackMagicHeld() => Input.GetKey(magicKey);

    // Movement
    
    public float GetHorizontalInput()
    {
        float value = 0f;
        if (GameInput.Instance != null)
        {
            if (GameInput.Instance.IsLeftActionPressed()) value -= 1f;
            if (GameInput.Instance.IsRightActionPressed()) value += 1f;
        }
        return Mathf.Clamp(value, -1f, 1f);
    }

    public float GetVerticalInput()
    {
        float value = 0f;
        if (GameInput.Instance != null)
        {
            if (GameInput.Instance.IsDownActionPressed()) value -= 1f;
            if (GameInput.Instance.IsUpActionPressed()) value += 1f;
        }
        return Mathf.Clamp(value, -1f, 1f);
    }

    public Vector2 GetMovementInput()
    {
        return new Vector2(GetHorizontalInput(), GetVerticalInput());
    }

    // Jump
    
    public bool GetJumpDown() => GameInput.Instance != null && GameInput.Instance.IsJumpActionPressed();
    public bool GetJumpHeld() => GameInput.Instance != null && GameInput.Instance.IsJumpActionHeld();
    public bool GetJumpUp() => GameInput.Instance != null && GameInput.Instance.IsJumpActionReleased();

    // Dash
    public bool GetDashDown() => GameInput.Instance != null && GameInput.Instance.IsDashActionPressed();

    // Pause/Menu  
    
    public bool GetPauseDown() => GameInput.Instance != null && GameInput.Instance.IsPauseActionPressed();

    // Utility  
    
    public void ToggleScheme()
    {
        activeScheme = activeScheme == ControlScheme.WASD_JUK 
            ? ControlScheme.Arrow_ZXC 
            : ControlScheme.WASD_JUK;
    }

    public bool GetInteractDown() => Input.GetKeyDown(interactKey);

    public bool GetExtractDown() => Input.GetKeyDown(extractKey);
    public bool GetInventoryDown() => Input.GetKeyDown(inventoryKey);
    public bool GetMapDown() => Input.GetKeyDown(mapKey);
    public bool GetCancelDown() => Input.GetKeyDown(cancelKey);
    public bool GetHealDown() => Input.GetKeyDown(healKey);
    public bool GetToolDown() => Input.GetKeyDown(toolKey);
}
