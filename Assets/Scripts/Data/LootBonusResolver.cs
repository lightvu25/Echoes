using UnityEngine;

/// <summary>
/// Static helper that gathers all runtime loot bonuses (Mind Garden, Meta Progression, Room Modifiers)
/// into a single LootBonuses struct for Just-In-Time evaluation.
/// ScriptableObjects are NEVER mutated — all bonuses are read from RunData at drop time.
/// </summary>
public static class LootBonusResolver
{
    /// <summary>
    /// Resolves all active loot bonuses from RunData and an optional room-level multiplier.
    /// Call this at the moment of dropping loot (JIT), not ahead of time.
    /// </summary>
    public static LootBonuses Resolve(float roomRelicMult = 1f, float roomEchoMult = 1f, float roomEquipmentMult = 1f)
    {
        RunData run = GameSession.Instance?.currentRun;

        return new LootBonuses
        {
            relicBonus      = run?.bonusRelicChance     ?? 0f,
            echoBonus       = run?.bonusEchoChance      ?? 0f,
            equipmentBonus  = run?.bonusEquipmentChance  ?? 0f,
            
            roomRelicMultiplier     = run != null ? run.currentLevelRelicMultiplier : 1f,
            roomEchoMultiplier      = run != null ? run.currentLevelEchoMultiplier : 1f,
            roomEquipmentMultiplier = run != null ? run.currentLevelEquipmentMultiplier : 1f
        };
    }
}
