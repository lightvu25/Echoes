using UnityEngine;

/// <summary>
/// Placed on a dead enemy, boss corpse, or environmental hazard.
/// Allows the player to "extract" a <see cref="MemoryItemData"/> element
/// a limited number of times before the source is depleted.
///
/// Attach to the same GameObject as the one that fires the trigger,
/// OR child it under the object — as long as it is reachable via
/// <see cref="Component.GetComponentInParent{T}"/> from the collider.
/// </summary>
public class MemorySource : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector Fields
    // -----------------------------------------------------------------------

    [Tooltip("The MemoryItemData that will be extracted and added to the player's inventory.")]
    [SerializeField] private MemoryItemData memoryToExtract;

    [Tooltip("How many times this source can be extracted before it depletes.")]
    [SerializeField] private int extractionUses = 1;

    [Tooltip("Optional VFX prefab played at the source's position on each extraction.")]
    [SerializeField] private GameObject extractVFXPrefab;

    [Tooltip("If true, destroy the GameObject on depletion. If false, only disable it.")]
    [SerializeField] private bool destroyOnDepletion = true;

    // -----------------------------------------------------------------------
    // Properties
    // -----------------------------------------------------------------------

    /// <summary>Whether this source still has memories available to extract.</summary>
    public bool IsAvailable => extractionUses > 0 && memoryToExtract != null;

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Extracts one <see cref="MemoryItemData"/> from this source.
    /// Decrements the use counter and handles depletion automatically.
    /// </summary>
    /// <returns>
    /// The <see cref="MemoryItemData"/> to be added to the player's inventory,
    /// or <c>null</c> if depleted or misconfigured.
    /// </returns>
    public MemoryItemData ExtractMemory()
    {
        if (!IsAvailable)
        {
            Debug.LogWarning($"MemorySource on '{gameObject.name}' is depleted or has no data assigned.");
            return null;
        }

        // --- Play VFX ---
        if (extractVFXPrefab != null)
            Instantiate(extractVFXPrefab, transform.position, Quaternion.identity);

        // --- Decrement uses ---
        extractionUses--;
        MemoryItemData extracted = memoryToExtract;

        // --- Depletion handling ---
        if (extractionUses <= 0)
        {
            if (destroyOnDepletion)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        return extracted;
    }
}
