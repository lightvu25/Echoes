using UnityEngine;

/// <summary>
/// ScriptableObject for configurable input mappings.
/// Wraps GameInput (New Input System) and provides dual control scheme support.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Input Config")]
public class InputConfig : ScriptableObject
{
    public enum ControlScheme
    {
        WASD_JUK,    // Set 1: WASD movement, J/U/K combat, L dash
        Arrow_ZXC    // Set 2: Arrow movement, Z/X/C combat, L-Shift dash
    }

    [Header("Active Scheme")]
    public ControlScheme activeScheme = ControlScheme.WASD_JUK;

    // ===== Combat Keys (these are NEW, not in GameInput) =====
    [Header("Scheme 1: WASD + JUK (Combat)")]
    public KeyCode wasd_Attack = KeyCode.J;
    public KeyCode wasd_Skill = KeyCode.U;
    public KeyCode wasd_Special = KeyCode.K;

    [Header("Scheme 2: Arrow + ZXC (Combat)")]
    public KeyCode arrow_Attack = KeyCode.Z;
    public KeyCode arrow_Skill = KeyCode.X;
    public KeyCode arrow_Special = KeyCode.C;

    // ===== Combat Input Properties =====
    public KeyCode AttackKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Attack : arrow_Attack;
    public KeyCode SkillKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Skill : arrow_Skill;
    public KeyCode SpecialKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Special : arrow_Special;

    // ===== Combat Input Methods =====
    public bool GetAttackDown() => Input.GetKeyDown(AttackKey);
    public bool GetAttackHeld() => Input.GetKey(AttackKey);
    public bool GetAttackUp() => Input.GetKeyUp(AttackKey);
    public bool GetSkillDown() => Input.GetKeyDown(SkillKey);
    public bool GetSpecialDown() => Input.GetKeyDown(SpecialKey);

    // ===== Movement (Wraps GameInput) =====
    
    /// <summary>
    /// Get horizontal movement input (-1 to 1).
    /// Uses GameInput's New Input System.
    /// </summary>
    public float GetHorizontalInput()
    {
        if (GameInput.Instance == null) return Input.GetAxisRaw("Horizontal");

        float value = 0f;
        if (GameInput.Instance.IsLeftActionPressed()) value -= 1f;
        if (GameInput.Instance.IsRightActionPressed()) value += 1f;
        return value;
    }

    /// <summary>
    /// Get vertical movement input (-1 to 1).
    /// Uses GameInput's New Input System.
    /// </summary>
    public float GetVerticalInput()
    {
        if (GameInput.Instance == null) return Input.GetAxisRaw("Vertical");

        float value = 0f;
        if (GameInput.Instance.IsDownActionPressed()) value -= 1f;
        if (GameInput.Instance.IsUpActionPressed()) value += 1f;
        return value;
    }

    /// <summary>
    /// Get movement input as Vector2.
    /// </summary>
    public Vector2 GetMovementInput()
    {
        return new Vector2(GetHorizontalInput(), GetVerticalInput());
    }

    // ===== Jump (Wraps GameInput) =====
    
    /// <summary>
    /// Check if jump was pressed this frame.
    /// </summary>
    public bool GetJumpDown()
    {
        if (GameInput.Instance == null) return Input.GetKeyDown(KeyCode.Space);
        return GameInput.Instance.IsJumpActionPressed();
    }

    /// <summary>
    /// Check if jump is being held.
    /// </summary>
    public bool GetJumpHeld()
    {
        // GameInput doesn't have a held check, fallback to legacy
        return Input.GetKey(KeyCode.Space);
    }

    /// <summary>
    /// Check if jump was released.
    /// </summary>
    public bool GetJumpUp()
    {
        return Input.GetKeyUp(KeyCode.Space);
    }

    // ===== Dash (Wraps GameInput) =====
    
    /// <summary>
    /// Check if dash was pressed this frame.
    /// </summary>
    public bool GetDashDown()
    {
        if (GameInput.Instance == null)
        {
            // Fallback based on scheme
            KeyCode dashKey = activeScheme == ControlScheme.WASD_JUK ? KeyCode.L : KeyCode.LeftShift;
            return Input.GetKeyDown(dashKey);
        }
        return GameInput.Instance.IsDashActionPressed();
    }

    // ===== Pause/Menu =====
    
    /// <summary>
    /// Check if pause/menu was pressed.
    /// </summary>
    public bool GetPauseDown()
    {
        if (GameInput.Instance == null) return Input.GetKeyDown(KeyCode.Escape);
        return GameInput.Instance.IsPauseActionPressed();
    }

    // ===== Utility =====
    
    /// <summary>
    /// Switch between control schemes.
    /// </summary>
    public void ToggleScheme()
    {
        activeScheme = activeScheme == ControlScheme.WASD_JUK 
            ? ControlScheme.Arrow_ZXC 
            : ControlScheme.WASD_JUK;
    }
}
