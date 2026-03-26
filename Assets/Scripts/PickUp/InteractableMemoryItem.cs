using UnityEngine;
using TMPro;

/// <summary>
/// Attach to a dropped Memory Item prefab in the world.
/// Inherits from InteractableObject to provide generic interaction logic.
/// </summary>
public class InteractableMemoryItem : InteractableObject
{
    // ===== Config =====
    [Header("Item Data")]
    [SerializeField] private MemoryItemData itemData;

    [Header("Feedback UI")]
    [Tooltip("Optional: text element for 'Need Healing!' feedback.")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 1.5f;

    // ===== State =====
    private float feedbackTimer = 0f;

    protected override void Update()
    {
        base.Update();
        HandleFeedbackTimer();
    }

    /// <summary>
    /// Triggered from the InteractableObject base class when the interact input is pressed.
    /// </summary>
    protected override void OnInteract()
    {
        TryPickUp();
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        // Let the base class handle toggling the prompt and boolean flags
        base.OnTriggerExit2D(other);
        
        if (other.CompareTag("Player"))
        {
            ClearFeedback();
        }
    }

    /// <summary>
    /// Attempts to add this item to the player's inventory.
    /// Checks capacity against unlocked slots and provides feedback on failure.
    /// </summary>
    private void TryPickUp()
    {
        MemoryInventorySystem inventory = MemoryInventorySystem.Instance;

        if (inventory == null)
        {
            Debug.LogWarning("[InteractableMemoryItem] MemoryInventorySystem.Instance is null.");
            return;
        }

        // Capacity check: current items must be strictly less than unlocked slots
        if (inventory.activeSlots.Count >= inventory.UnlockedSlots)
        {
            ShowFeedback("Need Healing!");
            return;
        }

        bool added = inventory.TryAddMemoryItem(itemData);

        if (added)
        {
            SetPromptVisible(false);
            Destroy(gameObject);
        }
        else
        {
            // Fallback fail case if the system rejected it for some other internal reason
            ShowFeedback("Inventory Full!");
        }
    }

    /// <summary>Show a temporary feedback message above the item.</summary>
    /// <param name="message">Message text to display.</param>
    private void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        feedbackTimer = feedbackDuration;
    }

    /// <summary>Hides the feedback text.</summary>
    private void ClearFeedback()
    {
        if (feedbackText == null) return;

        feedbackText.gameObject.SetActive(false);
        feedbackTimer = 0f;
    }

    private void HandleFeedbackTimer()
    {
        if (feedbackTimer <= 0f) return;

        feedbackTimer -= Time.deltaTime;
        if (feedbackTimer <= 0f)
        {
            ClearFeedback();
        }
    }
}
