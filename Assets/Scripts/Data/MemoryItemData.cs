using UnityEngine;

public enum ElementType
{
    None,
    Fire,
    Poison,
    Lightning,
    Ice,
    Wind,
    Earth
}

[CreateAssetMenu(fileName = "New Memory Item", menuName = "Data/Memory Item")]
public class MemoryItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public GameObject dropPrefab;
    public ElementType elementType;
}
