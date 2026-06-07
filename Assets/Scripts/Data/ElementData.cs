using UnityEngine;

/// <summary>
/// Defines the elemental type of an attack modifier that the player equips.
/// Inherits common identity fields from <see cref="ItemBaseData"/>.
/// </summary>
[CreateAssetMenu(fileName = "New Memory Element", menuName = "Data/Memory Element")]
public class ElementData : ItemBaseData
{
    // ------------------------------------------------------------------ //
    //  ItemBaseData contract                                               //
    // ------------------------------------------------------------------ //

    /// <inheritdoc/>
    public override ItemCategory Category => ItemCategory.Element;

    // ------------------------------------------------------------------ //
    //  Element-specific fields                                             //
    // ------------------------------------------------------------------ //

    [Header("Element Info")]
    [Tooltip("Which element type this item applies.")]
    public ElementType elementType;

    [Header("Damage Stats")]
    [Tooltip("Flat damage added on top of the player's base attack.")]
    public int baseAddedDamage;

    [Tooltip("Per-level damage scaling multiplier (additive).")]
    public float levelScalingFactor = 0.1f;

    [Header("Status Effect")]
    public bool hasStatusEffect;

    [Range(0f, 1f)]
    [Tooltip("Probability (0–1) that this element inflicts its status on hit.")]
    public float statusProcCoefficient;

    // public BuffData associatedDebuff;
}
