using UnityEngine;

/// <summary>
/// Defines a passive relic item the player can equip.
/// Relics grant persistent stat bonuses and optional elemental synergy effects.
/// Inherits common identity fields from <see cref="ItemBaseData"/>.
/// </summary>
[CreateAssetMenu(fileName = "New Relic", menuName = "Data/Relic")]
public class RelicData : ItemBaseData
{
    // ------------------------------------------------------------------ //
    //  ItemBaseData contract                                               //
    // ------------------------------------------------------------------ //

    /// <inheritdoc/>
    public override ItemCategory Category => ItemCategory.Relic;

    // ------------------------------------------------------------------ //
    //  Passive stat bonuses                                                //
    // ------------------------------------------------------------------ //

    [Header("Passive Stats")]
    [Tooltip("Flat damage bonus added to every attack while this relic is equipped.")]
    public float flatDamageBonus;

    [Tooltip("Additional critical hit chance (0.0–1.0) granted by this relic.")]
    [Range(0f, 1f)]
    public float critChanceBonus;

    [Tooltip("Multiplier applied to the player's Max HP (e.g. 1.1 = +10% Max HP).")]
    public float maxHpMultiplier = 1f;

    // ------------------------------------------------------------------ //
    //  Elemental synergy                                                   //
    // ------------------------------------------------------------------ //

    [Header("Elemental Synergy")]
    [Tooltip("If set, this relic's bonus only activates when the player has an element of this type equipped.")]
    public ElementType synergyElement = ElementType.None;
}
