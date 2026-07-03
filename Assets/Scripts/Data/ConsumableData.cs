using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Data/Consumable")]
public class ConsumableData : ItemBaseData
{
    public override ItemCategory Category => ItemCategory.Item;

    [Header("Consumable Effect")]
    [Tooltip("Flat HP restored when this item is used. 0 means no healing.")]
    public int healAmount;

    [Tooltip("Optional buff applied to the player on use. Leave null for heal-only items.")]
    public BuffData buffOnUse;
}
