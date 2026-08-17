using UnityEngine;

/// <summary>
/// Centralized system for applying stat bonuses (Vitality, Sorcery, Resonance)
/// from ANY source — shrine blessings, relics, or future systems — to the player's
/// RunData and live HealthSystem.
///
/// All Vitality gains share a single decay pool (<see cref="RunData.bonusVitality"/>),
/// ensuring consistent diminishing returns regardless of source.
///
/// Formula: HP_n = BlessingCalculator.CalculateStackBonus(VitalityBaseHP, n)
///          where n = current bonusVitality + 1 for each new point added.
/// </summary>
public static class StatBonusSystem
{
    /// <summary>Base HP granted by the very first Vitality stack.</summary>
    public const int VitalityBaseHP = 50;

    /// <summary>Minimum HP granted per Vitality stack (floor to prevent zero gains).</summary>
    public const int VitalityMinHP = 5;

    // ------------------------------------------------------------------ //
    //  Vitality                                                            //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Adds <paramref name="vitalityAmount"/> Vitality stacks to the run,
    /// calculates HP gain via Harmonic Decay, and applies it to the live
    /// <see cref="HealthSystem"/>. Updates <see cref="RunData.bonusVitality"/>.
    /// </summary>
    /// <param name="run">The active RunData. Must not be null.</param>
    /// <param name="hs">The player's HealthSystem. May be null (stat stored but HP not applied).</param>
    /// <param name="vitalityAmount">Number of Vitality points to add.</param>
    /// <returns>Total HP granted.</returns>
    public static int ApplyVitalityBonus(RunData run, HealthSystem hs, int vitalityAmount)
    {
        if (run == null || vitalityAmount <= 0) return 0;

        int totalHpGain = 0;
        for (int i = 0; i < vitalityAmount; i++)
        {
            // Stack index is 1-based: the (bonusVitality + i + 1)-th total Vitality point
            int stackIndex = run.bonusVitality + i + 1;
            totalHpGain += Mathf.Max(
                BlessingCalculator.CalculateStackBonus(VitalityBaseHP, stackIndex),
                VitalityMinHP);
        }

        run.bonusVitality += vitalityAmount;

        if (hs != null && totalHpGain > 0)
        {
            hs.ModifyMaxHP(totalHpGain);
            hs.Heal(totalHpGain);
        }

        return totalHpGain;
    }

    /// <summary>
    /// Removes <paramref name="vitalityAmount"/> Vitality stacks from the run,
    /// reverses the exact HP that was granted for those stacks, and applies the
    /// reduction to the live <see cref="HealthSystem"/>. Updates <see cref="RunData.bonusVitality"/>.
    ///
    /// The HP loss is computed deterministically from the Harmonic Decay curve,
    /// matching what <see cref="ApplyVitalityBonus"/> would have granted.
    /// </summary>
    /// <param name="run">The active RunData. Must not be null.</param>
    /// <param name="hs">The player's HealthSystem. May be null.</param>
    /// <param name="vitalityAmount">Number of Vitality points to remove.</param>
    /// <returns>Total HP removed from max HP.</returns>
    public static int RemoveVitalityBonus(RunData run, HealthSystem hs, int vitalityAmount)
    {
        if (run == null || vitalityAmount <= 0) return 0;

        // Clamp removal to what is actually available
        int actualRemoval = Mathf.Min(vitalityAmount, run.bonusVitality);
        if (actualRemoval <= 0) return 0;

        int newTotal = run.bonusVitality - actualRemoval;
        int totalHpLoss = 0;

        // Reconstruct the HP that was granted for each stack we are removing.
        // Stack indices [newTotal+1 .. run.bonusVitality] (1-based).
        for (int i = newTotal; i < run.bonusVitality; i++)
        {
            int stackIndex = i + 1;
            totalHpLoss += Mathf.Max(
                BlessingCalculator.CalculateStackBonus(VitalityBaseHP, stackIndex),
                VitalityMinHP);
        }

        run.bonusVitality = newTotal;

        if (hs != null && totalHpLoss > 0)
        {
            hs.ModifyMaxHP(-totalHpLoss, false, false);
        }

        return totalHpLoss;
    }
}
