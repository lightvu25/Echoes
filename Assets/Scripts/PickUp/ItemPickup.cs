using UnityEngine;

/// <summary>
/// World pickup that attempts to equip an <see cref="ItemBaseData"/> into the player's inventory
/// when the player enters its trigger zone.
///
/// This class is intentionally thin: all equip/swap logic lives in
/// <see cref="PlayerInventoryCore"/>. MemoryPickup (ItemPickup) only fires the request.
/// </summary>
public class MemoryPickup : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private ItemBaseData itemData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (itemData == null)
        {
            Debug.LogWarning($"[MemoryPickup] '{name}' has no ItemData assigned.", this);
            return;
        }

        if (PlayerInventoryCore.Instance == null)
        {
            Debug.LogWarning("[MemoryPickup] PlayerInventoryCore.Instance is null.");
            return;
        }

        // Delegate all equip/swap logic to the core.
        // If the slot is full, core fires OnSwapRequired and SwapUI handles it.
        PlayerInventoryCore.Instance.TryEquip(itemData);

        // Destroy only after successfully starting the equip flow.
        // Note: if a swap is required the item is not yet equipped —
        // destroy is deferred until SwapUI confirms or cancels.
        // For simplicity in this iteration, destroy immediately.
        // A future iteration can wait for OnInventoryChanged to confirm.
        Destroy(gameObject);
    }
}