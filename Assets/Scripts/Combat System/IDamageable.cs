using UnityEngine;

/// <summary>
/// Interface for any entity that can receive damage.
/// Implemented by players, enemies, and destructible objects.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Apply damage to this entity.
    /// </summary>
    /// <param name="damageInfo">Contains all damage calculation data</param>
    void TakeDamage(DamageInfo damageInfo);

    /// <summary>
    /// Check if this entity is dead.
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// Get the transform for knockback direction calculations.
    /// </summary>
    Transform Transform { get; }

    /// <summary>
    /// Get defense stat for damage reduction calculation.
    /// </summary>
    float Defense { get; }
}
