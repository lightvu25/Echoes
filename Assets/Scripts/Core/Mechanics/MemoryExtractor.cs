using UnityEngine;

/// <summary>
/// Player-side component that detects nearby <see cref="MemorySource"/> objects
/// and extracts their <see cref="MemoryItemData"/> into the player's
/// <see cref="MemoryInventorySystem"/> on trigger contact.
///
/// HOW IT WORKS:
///   1. Attach this component to the Player GameObject (or a Trigger child).
///   2. Set the Trigger Collider's layer/tag filter so only MemorySource
///      colliders enter it (use a dedicated "MemorySource" layer on sources).
///   3. When the player walks into a MemorySource trigger, extraction fires
///      automatically — no button press required.
///
/// If you want button-press extraction instead, hook into the Interact input
/// action inside <see cref="TryExtract"/> and call it from an input handler.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MemoryExtractor : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector Fields
    // -----------------------------------------------------------------------

    [Tooltip("Tag on the MemorySource colliders. Default: 'MemorySource'.\n" +
             "Add this tag in Edit → Project Settings → Tags and Layers.")]
    [SerializeField] private string memorySourceTag = "MemorySource";

    // -----------------------------------------------------------------------
    // Trigger — Automatic (proximity) extraction
    // -----------------------------------------------------------------------

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(memorySourceTag)) return;

        // Walk up the hierarchy to find the MemorySource component —
        // the collider might be on a child of the actual MemorySource owner.
        MemorySource source = other.GetComponentInParent<MemorySource>();
        if (source == null) return;

        TryExtract(source);
    }

    // -----------------------------------------------------------------------
    // Core Extraction Logic
    // -----------------------------------------------------------------------

    /// <summary>
    /// Attempts to extract a <see cref="MemoryItemData"/> from the given
    /// <paramref name="source"/> and add it to the player's inventory.
    /// Safe to call multiple times; both MemorySource and MemoryInventorySystem
    /// guard against excess extractions.
    /// </summary>
    /// <param name="source">The source to extract from.</param>
    public void TryExtract(MemorySource source)
    {
        if (source == null || !source.IsAvailable) return;
        if (MemoryInventorySystem.Instance == null)
        {
            Debug.LogWarning("MemoryExtractor: MemoryInventorySystem.Instance is null.");
            return;
        }

        MemoryItemData item = source.ExtractMemory();
        if (item == null) return;

        bool added = MemoryInventorySystem.Instance.TryAddMemoryItem(item);

        if (!added)
        {
            // Inventory was full — the item data is simply not added.
            // You can extend this to show a UI notification here.
            Debug.Log($"MemoryExtractor: Inventory full — could not add '{item.itemName}'.");
        }
    }
}
