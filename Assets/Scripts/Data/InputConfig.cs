using UnityEngine;

/// <summary>
/// ScriptableObject for configurable input mappings.
/// Supports dual control schemes (WASD+JUK and Arrow+ZXC).
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

    // ===== WASD+JUK Scheme =====
    [Header("Scheme 1: WASD + JUK")]
    public KeyCode wasd_Attack = KeyCode.J;
    public KeyCode wasd_Skill = KeyCode.U;
    public KeyCode wasd_Special = KeyCode.K;
    public KeyCode wasd_Dash = KeyCode.L;
    public KeyCode wasd_Jump = KeyCode.Space;

    // ===== Arrow+ZXC Scheme =====
    [Header("Scheme 2: Arrow + ZXC")]
    public KeyCode arrow_Attack = KeyCode.Z;
    public KeyCode arrow_Skill = KeyCode.X;
    public KeyCode arrow_Special = KeyCode.C;
    public KeyCode arrow_Dash = KeyCode.LeftShift;
    public KeyCode arrow_Jump = KeyCode.Space;

    // ===== Input Query Methods =====

    public KeyCode AttackKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Attack : arrow_Attack;
    public KeyCode SkillKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Skill : arrow_Skill;
    public KeyCode SpecialKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Special : arrow_Special;
    public KeyCode DashKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Dash : arrow_Dash;
    public KeyCode JumpKey => activeScheme == ControlScheme.WASD_JUK ? wasd_Jump : arrow_Jump;

    /// <summary>
    /// Check if attack button is pressed this frame.
    /// </summary>
    public bool GetAttackDown() => Input.GetKeyDown(AttackKey);

    /// <summary>
    /// Check if attack button is held.
    /// </summary>
    public bool GetAttackHeld() => Input.GetKey(AttackKey);

    /// <summary>
    /// Check if attack button was released.
    /// </summary>
    public bool GetAttackUp() => Input.GetKeyUp(AttackKey);

    /// <summary>
    /// Check if skill button is pressed.
    /// </summary>
    public bool GetSkillDown() => Input.GetKeyDown(SkillKey);

    /// <summary>
    /// Check if special button is pressed.
    /// </summary>
    public bool GetSpecialDown() => Input.GetKeyDown(SpecialKey);

    /// <summary>
    /// Check if dash button is pressed.
    /// </summary>
    public bool GetDashDown() => Input.GetKeyDown(DashKey);

    /// <summary>
    /// Check if jump button is pressed.
    /// </summary>
    public bool GetJumpDown() => Input.GetKeyDown(JumpKey);

    /// <summary>
    /// Get movement input based on active scheme.
    /// </summary>
    public Vector2 GetMovementInput()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (activeScheme == ControlScheme.WASD_JUK)
        {
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
        }

        return new Vector2(horizontal, vertical);
    }

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
