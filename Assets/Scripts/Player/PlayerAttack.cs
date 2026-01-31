using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player attack input and combo system.
/// BlazBlue-style: Basic, Heavy, Dash, and Air attacks.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    public event EventHandler<AttackEventArgs> OnAttackStarted;
    public event EventHandler<AttackEventArgs> OnAttackEnded;

    public class AttackEventArgs : EventArgs
    {
        public AttackType attackType;
        public int comboStep;
    }

    public enum AttackType
    {
        Basic,      // Tap attack
        Heavy,      // Hold attack
        Dash,       // Attack during dash
        Air         // Attack in air
    }

    [Header("References")]
    [SerializeField] private InputConfig inputConfig;
    [SerializeField] private CombatStats combatStats;
    [SerializeField] private AttackHitbox attackHitbox;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Basic Attack Combo")]
    [Tooltip("Number of hits in basic combo chain")]
    [SerializeField] private int maxComboSteps = 3;
    [Tooltip("Time window to input next combo hit")]
    [SerializeField] private float comboWindow = 0.4f;
    [Tooltip("Damage multiplier per combo step")]
    [SerializeField] private float[] comboDamageMultipliers = { 1f, 1.2f, 1.5f };

    [Header("Attack Timings")]
    [SerializeField] private float basicAttackDuration = 0.3f;
    [SerializeField] private float heavyAttackDuration = 0.5f;
    [SerializeField] private float dashAttackDuration = 0.25f;
    [SerializeField] private float airAttackDuration = 0.35f;

    [Header("Heavy Attack")]
    [Tooltip("Hold time to trigger heavy attack")]
    [SerializeField] private float heavyAttackHoldTime = 0.3f;
    [SerializeField] private float heavyAttackDamageMultiplier = 2f;

    [Header("Proc Coefficients")]
    [SerializeField] private float basicProcCoef = 1f;
    [SerializeField] private float heavyProcCoef = 1.5f;
    [SerializeField] private float dashProcCoef = 0.7f;
    [SerializeField] private float airProcCoef = 0.8f;

    // State
    private bool isAttacking = false;
    private int currentComboStep = 0;
    private float comboTimer = 0f;
    private float attackHoldTime = 0f;
    private bool comboQueued = false;
    private AttackType currentAttackType;

    private void Update()
    {
        HandleComboTimer();
        HandleAttackInput();
    }

    private void HandleComboTimer()
    {
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                // Combo reset
                currentComboStep = 0;
            }
        }
    }

    private void HandleAttackInput()
    {
        if (inputConfig == null) return;

        // Track hold time for heavy attack
        if (inputConfig.GetAttackHeld())
        {
            attackHoldTime += Time.deltaTime;
        }

        // Attack button pressed
        if (inputConfig.GetAttackDown())
        {
            attackHoldTime = 0f;

            if (!isAttacking)
            {
                DetermineAndExecuteAttack();
            }
            else
            {
                // Queue next combo hit
                comboQueued = true;
            }
        }

        // Attack button released (check for heavy attack)
        if (inputConfig.GetAttackUp())
        {
            if (attackHoldTime >= heavyAttackHoldTime && !isAttacking)
            {
                ExecuteAttack(AttackType.Heavy);
            }
            attackHoldTime = 0f;
        }
    }

    private void DetermineAndExecuteAttack()
    {
        AttackType type;

        // Check conditions for attack type
        if (playerMovement != null && playerMovement.isDashing)
        {
            type = AttackType.Dash;
        }
        else if (playerMovement != null && !IsGrounded())
        {
            type = AttackType.Air;
        }
        else
        {
            type = AttackType.Basic;
        }

        ExecuteAttack(type);
    }

    private void ExecuteAttack(AttackType type)
    {
        if (isAttacking) return;

        currentAttackType = type;
        StartCoroutine(AttackRoutine(type));
    }

    private IEnumerator AttackRoutine(AttackType type)
    {
        isAttacking = true;
        comboQueued = false;

        // Determine attack parameters
        float duration;
        float damageMultiplier;
        float procCoef;

        switch (type)
        {
            case AttackType.Heavy:
                duration = heavyAttackDuration;
                damageMultiplier = heavyAttackDamageMultiplier;
                procCoef = heavyProcCoef;
                currentComboStep = 0; // Heavy resets combo
                break;

            case AttackType.Dash:
                duration = dashAttackDuration;
                damageMultiplier = 1.3f;
                procCoef = dashProcCoef;
                currentComboStep = 0;
                break;

            case AttackType.Air:
                duration = airAttackDuration;
                damageMultiplier = 1.1f;
                procCoef = airProcCoef;
                break;

            default: // Basic
                duration = basicAttackDuration;
                damageMultiplier = GetComboDamageMultiplier();
                procCoef = basicProcCoef;
                break;
        }

        // Fire event
        OnAttackStarted?.Invoke(this, new AttackEventArgs
        {
            attackType = type,
            comboStep = currentComboStep
        });

        // Calculate damage
        int baseDamage = combatStats != null ? combatStats.baseAttack : 10;
        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

        // Activate hitbox
        if (attackHitbox != null)
        {
            attackHitbox.Activate(finalDamage, procCoef);
        }

        // Wait for attack duration
        yield return new WaitForSeconds(duration);

        // Attack ended
        isAttacking = false;

        // Handle combo progression
        if (type == AttackType.Basic)
        {
            currentComboStep++;
            if (currentComboStep >= maxComboSteps)
            {
                currentComboStep = 0;
            }
            comboTimer = comboWindow;
        }

        // Fire end event
        OnAttackEnded?.Invoke(this, new AttackEventArgs
        {
            attackType = type,
            comboStep = currentComboStep
        });

        // Check for queued combo
        if (comboQueued && type == AttackType.Basic)
        {
            comboQueued = false;
            DetermineAndExecuteAttack();
        }
    }

    private float GetComboDamageMultiplier()
    {
        if (comboDamageMultipliers == null || comboDamageMultipliers.Length == 0)
            return 1f;

        int index = Mathf.Clamp(currentComboStep, 0, comboDamageMultipliers.Length - 1);
        return comboDamageMultipliers[index];
    }

    private bool IsGrounded()
    {
        if (playerMovement != null)
        {
            return playerMovement.LastOnGroundTime > 0;
        }
        return true;
    }

    /// <summary>
    /// Check if currently in attack animation.
    /// </summary>
    public bool IsAttacking => isAttacking;

    /// <summary>
    /// Get current combo step (0-indexed).
    /// </summary>
    public int CurrentComboStep => currentComboStep;
}
