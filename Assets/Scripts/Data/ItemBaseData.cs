using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Abstract base ScriptableObject for all equippable/droppable items in Project Echoes.
/// Provides the common identity fields shared by Elements, Relics, and Consumables.
/// Child classes inherit this and add category-specific stats.
/// </summary>
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

    /// <summary>The inventory category this item belongs to.</summary>
    public abstract ItemCategory Category { get; }
}
