using UnityEngine;
using System.Collections.Generic;

public enum DamageSourceType
{
    Unknown,
    Attack,
    ArcChain,
    BlackHole,
    OblivionSelfDamage,
    VoidDetonate,
    Burn,
    BombAttack,
    Suicide,
    DashAttack,
    EnemyAttack,
    MeleeAttack,
    Projectile,
    Environment_Thorns,
    ElectrifiedWater,
    FallDamage,
    PlayerProjectile,
    PlungeAttack,
    PlungeSelfDamage,
    PlungeFall
}

/// <summary>
/// Contains all data needed for damage calculation.
/// Used in the two-stage damage pipeline.
/// </summary>
[System.Serializable]
public struct DamageInfo
{
    // ===== Stage 1: Base Damage Values =====
    
    /// <summary>
    /// Base damage before any modifiers.
    /// </summary>
    public int baseDamage;

    /// <summary>
    /// Flat damage added to base (e.g., weapon bonus).
    /// </summary>
    public int flatBonus;

    /// <summary>
    /// Sum of linear modifiers (same-source stacking).
    /// Example: 3x damage items at 10% each = 0.30
    /// </summary>
    public float linearModifierSum;

    /// <summary>
    /// Product of multiplicative modifiers (different-source stacking).
    /// Example: Crit 1.5x × Skill 1.2x = 1.8
    /// </summary>
    public float multiplicativeStack;

    // ===== Stage 2: Proc System =====

    /// <summary>
    /// Affects on-hit item trigger rates (0.0 to 1.0).
    /// Lower for multi-hit attacks, higher for single hits.
    /// </summary>
    public float procCoefficient;

    // ===== Combat Feedback =====

    /// <summary>
    /// Direction to apply knockback.
    /// </summary>
    public Vector2 knockbackDirection;

    /// <summary>
    /// Force of knockback impulse.
    /// </summary>
    public float knockbackForce;

    /// <summary>
    /// Duration to freeze time on hit (hit-stop effect).
    /// </summary>
    public float hitFreezeTime;

    /// <summary>
    /// The entity that dealt this damage.
    /// </summary>
    public GameObject attacker;

    /// <summary>
    /// Source identifier for item triggers (e.g., "BasicAttack", "Skill1").
    /// </summary>
    public DamageSourceType damageSource;

    /// <summary>
    /// Is this a critical hit?
    /// </summary>
    public bool isCritical;

    /// <summary>
    /// Nguyên tố hiện tại đang sử dụng (Dành cho hệ thống Echo)
    /// </summary>
    public EchoData activeEcho;

    /// <summary>
    /// Cấp độ của Player khi gây sát thương
    /// </summary>
    public int playerLevel;
    public bool isTrueDamage;
    public bool isPiercing;

    // ===== Factory Methods =====

    /// <summary>
    /// Create a basic damage info with default values.
    /// </summary>
    public static DamageInfo Create(int baseDamage, GameObject attacker)
    {
        return new DamageInfo
        {
            baseDamage = baseDamage,
            flatBonus = 0,
            linearModifierSum = 0f,
            multiplicativeStack = 1f,
            procCoefficient = 1f,
            knockbackDirection = Vector2.zero,
            knockbackForce = 0f,
            hitFreezeTime = 0f,
            attacker = attacker,
            damageSource = DamageSourceType.Unknown,
            isCritical = false,
            isTrueDamage = false,
            isPiercing = false
        };
    }

    /// <summary>
    /// Create damage info with knockback.
    /// </summary>
    public static DamageInfo CreateWithKnockback(
        int baseDamage, 
        GameObject attacker, 
        Vector2 knockbackDir, 
        float knockbackForce)
    {
        var info = Create(baseDamage, attacker);
        info.knockbackDirection = knockbackDir.normalized;
        info.knockbackForce = knockbackForce;
        return info;
    }
}
