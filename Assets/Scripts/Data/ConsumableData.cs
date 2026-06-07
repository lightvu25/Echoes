using UnityEngine;

/// <summary>
/// Defines a consumable one-use item the player can equip and activate.
/// Consumables typically restore HP or apply a short-duration buff.
/// Inherits common identity fields from <see cref="ItemBaseData"/>.
/// </summary>
[CreateAssetMenu(fileName = "New Consumable", menuName = "Data/Consumable")]
public class ConsumableData : ItemBaseData
{
    // ------------------------------------------------------------------ //
    //  ItemBaseData contract                                               //
    // ------------------------------------------------------------------ //

    /// <inheritdoc/>
    public override ItemCategory Category => ItemCategory.Item;

    // ------------------------------------------------------------------ //
    //  Consumable-specific fields                                          //
    // ------------------------------------------------------------------ //

    [Header("Consumable Effect")]
    [Tooltip("Flat HP restored when this item is used. 0 means no healing.")]
    public int healAmount;

    [Tooltip("Optional buff applied to the player on use. Leave null for heal-only items.")]
    public BuffData buffOnUse;
}
