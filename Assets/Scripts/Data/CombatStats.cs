using UnityEngine;

/// <summary>
/// ScriptableObject containing base combat stats.
/// Used by players, enemies, and NPCs.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Combat Stats")]
public class CombatStats : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Maximum health points")]
    public int maxHP = 100;

    [Header("Defense")]
    [Tooltip("Defense value for armor formula. Higher = less damage taken.")]
    public float defense = 0f;

    [Header("Attack")]
    [Tooltip("Base attack damage")]
    public int baseAttack = 10;

    [Tooltip("Critical hit chance (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float critChance = 0.05f;

    [Tooltip("Critical hit damage multiplier")]
    public float critMultiplier = 1.5f;

    [Header("Combat Feedback")]
    [Tooltip("Knockback force when dealing damage")]
    public float knockbackForce = 5f;

    [Tooltip("Duration of hit-freeze effect")]
    public float hitFreezeTime = 0.05f;

    [Header("Invincibility")]
    [Tooltip("Duration of i-frames after taking damage")]
    public float iFrameDuration = 0.5f;

    [Header("Resources")]
    [Tooltip("Maximum MP for skills")]
    public int maxMP = 100;

    [Tooltip("MP regeneration per second")]
    public float mpRegenRate = 5f;

    [Tooltip("Maximum SP for special moves")]
    public int maxSP = 100;

    /// <summary>
    /// Create a DamageInfo from these stats.
    /// </summary>
    public DamageInfo CreateBaseDamageInfo(GameObject attacker, string source = "BasicAttack")
    {
        bool isCrit = Random.value < critChance;
        float multiplier = isCrit ? critMultiplier : 1f;

        return new DamageInfo
        {
            baseDamage = baseAttack,
            flatBonus = 0,
            linearModifierSum = 0f,
            multiplicativeStack = multiplier,
            procCoefficient = 1f,
            knockbackDirection = Vector2.zero,
            knockbackForce = knockbackForce,
            hitFreezeTime = hitFreezeTime,
            attacker = attacker,
            damageSource = source,
            isCritical = isCrit
        };
    }
}
