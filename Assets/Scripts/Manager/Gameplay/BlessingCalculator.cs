using UnityEngine;

public static class BlessingCalculator
{
    /// <summary>
    /// Calculates the stack bonus for blessings using Harmonic Decay formula.
    /// V_n = baseValue / (n^decayFactor)
    /// </summary>
    /// <param name="baseValue">The base bonus value for the first stack.</param>
    /// <param name="stackCount">The current stack index (1-based, e.g., 1 for the first time).</param>
    /// <param name="decayFactor">The power factor for the decay curve.</param>
    /// <returns>The calculated bonus rounded to the nearest integer.</returns>
    public static int CalculateStackBonus(int baseValue, int stackCount, float decayFactor = 1.0f)
    {
        if (stackCount <= 0) return baseValue;
        float decayAmount = Mathf.Pow(stackCount, decayFactor);
        return Mathf.RoundToInt(baseValue / decayAmount);
    }
}
