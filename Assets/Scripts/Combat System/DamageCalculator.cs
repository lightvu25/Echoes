using UnityEngine;

/// <summary>
/// Static utility class for damage calculation.
/// Implements the two-stage damage pipeline.
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// Constant used in armor formula for diminishing returns.
    /// Higher value = defense is less effective.
    /// </summary>
    private const float ARMOR_CONSTANT = 100f;

    /// <summary>
    /// Minimum damage that can be dealt (prevents complete immunity).
    /// </summary>
    private const int MINIMUM_DAMAGE = 1;

    /// <summary>
    /// Calculate final damage after all modifiers and defense.
    /// Formula: (Base + Flat) × (1 + Linear) × Multiplicative × (1 - DefenseReduction)
    /// </summary>
    /// <param name="info">Damage info with all modifiers</param>
    /// <param name="targetDefense">Target's defense stat</param>
    /// <returns>Final damage as integer</returns>
    public static int CalculateFinalDamage(DamageInfo info, float targetDefense)
    {
        // Stage 1: Apply modifiers
        float baseDamage = info.baseDamage + info.flatBonus;
        float linearMultiplier = 1f + info.linearModifierSum;
        float modifiedDamage = baseDamage * linearMultiplier * info.multiplicativeStack;

        // Apply defense reduction (armor formula with diminishing returns)
        float defenseReduction = CalculateDefenseReduction(targetDefense);
        float finalDamage = modifiedDamage * (1f - defenseReduction);

        // Ensure minimum damage
        return Mathf.Max(MINIMUM_DAMAGE, Mathf.RoundToInt(finalDamage));
    }

    /// <summary>
    /// Calculate defense reduction using armor formula.
    /// Formula: Defense / (Defense + Constant)
    /// This provides natural diminishing returns.
    /// </summary>
    /// <param name="defense">Target's defense value</param>
    /// <returns>Damage reduction as percentage (0.0 to ~0.99)</returns>
    public static float CalculateDefenseReduction(float defense)
    {
        if (defense <= 0f) return 0f;
        return defense / (defense + ARMOR_CONSTANT);
    }

    /// <summary>
    /// Calculate damage without defense (for display/preview).
    /// </summary>
    public static int CalculateRawDamage(DamageInfo info)
    {
        float baseDamage = info.baseDamage + info.flatBonus;
        float linearMultiplier = 1f + info.linearModifierSum;
        float modifiedDamage = baseDamage * linearMultiplier * info.multiplicativeStack;
        return Mathf.RoundToInt(modifiedDamage);
    }

    /// <summary>
    /// Check if a proc should trigger based on chance and coefficient.
    /// </summary>
    /// <param name="baseChance">Item's base proc chance (0.0 to 1.0)</param>
    /// <param name="procCoefficient">Attack's proc coefficient (0.0 to 1.0)</param>
    /// <returns>True if proc should trigger</returns>
    public static bool ShouldProc(float baseChance, float procCoefficient)
    {
        float effectiveChance = baseChance * procCoefficient;
        return Random.value < effectiveChance;
    }
}
