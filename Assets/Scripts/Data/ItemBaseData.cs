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

    public abstract ItemCategory Category { get; }
}
