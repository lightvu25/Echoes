/// <summary>
/// Identifies which inventory category an item belongs to.
/// Used by PlayerInventoryCore, SwapUI, and SlotUnlockPanel to route items
/// to the correct slot list without any hard-coded type checks.
/// </summary>
public enum ItemCategory
{
    Element,
    Relic,
    Item
}
