using UnityEngine;

[CreateAssetMenu(menuName = "Enemy Data/Evolution Tier")]
public class EvolutionTierData : ScriptableObject
{
    [Header("Tier Info")]
    public string tierName = "";

    [Tooltip("Number of kills required to reach this tier during the current run.")]
    public int requiredKills = 0;

    [Header("Stat Multipliers")]
    [Tooltip("Multiplier applied to the base telegraph duration (lower = faster attacks).")]
    public float telegraphDurationMultiplier = 1f;

    [Tooltip("Multiplier applied to the base attack cooldown (lower = attacks more often).")]
    public float attackCooldownMultiplier = 1f;

    [Tooltip("Multiplier applied to the base vision range (higher = sees further).")]
    public float visionRangeMultiplier = 1f;

    [Header("Abilities")]
    [Tooltip("If true, enemies at this tier can backstep away from the player after attacking.")]
    public bool canBackstep = false;

    [Tooltip("If true, this enemy will immediately know the player's location regardless of distance.")]
    public bool isGlobalAggro = false;

    [Tooltip("If true, when this enemy spots the player, it will alert nearby enemies.")]
    public bool canShareVision = false;

    [Header("Visuals")]
    [Tooltip("Persistent icon displayed above enemies while this tier's global shared vision is active.")]
    public Sprite sharedVisionIcon;
}
