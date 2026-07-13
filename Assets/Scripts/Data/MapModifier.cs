using UnityEngine;

/// <summary>
/// Defines a single map modifier that can alter procedural generation parameters.
/// Used by MemoryNodeData to describe how a chosen memory path affects the next level.
/// </summary>
[System.Serializable]
public class MapModifier
{
    public string modifierName;
    public ModifierType type;
    public float value;

    public enum ModifierType
    {
        EnemyDensity,
        RoomCount,
        TrapFrequency,
        LootMultiplier,
        BossEnabled
    }
}
