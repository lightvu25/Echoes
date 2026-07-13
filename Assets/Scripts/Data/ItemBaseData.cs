using UnityEngine;
using UnityEngine.Serialization;

public abstract class ItemBaseData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique string ID used for save/load serialisation.")]
    public string itemID;

    [Tooltip("Display name shown in the UI.")]
    [FormerlySerializedAs("elementName")]
    public string itemName;

    [Tooltip("Icon shown in inventory slots and the swap popup.")]
    [FormerlySerializedAs("elementIcon")]
    public Sprite itemIcon;

    [Tooltip("Short flavour/mechanical description for tooltips.")]
    [TextArea(2, 4)]
    public string description;

    [Header("World")]
    [Tooltip("Prefab spawned into the world when this item is dropped.")]
    public GameObject dropPrefab;

    [Header("Shop & Economy")]
    [Tooltip("Tier rating used for drop generation and shop pricing (e.g., Tier 1, Tier 2).")]
    public int itemTier = 1;

    [Tooltip("Base gold price of this item in the shop.")]
    public int basePrice = 50;

    public abstract ItemCategory Category { get; }
}
