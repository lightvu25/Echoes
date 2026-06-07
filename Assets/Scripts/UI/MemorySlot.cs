using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns the visual and animation state of a single Memory Slot in the
/// Health-as-Inventory UI.  StatsUI acts as the manager and calls these
/// methods; this component knows nothing about inventory counts or events.
/// </summary>
public class MemorySlot : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector Fields
    // -----------------------------------------------------------------------

    [Tooltip("Background / mask Image for the slot shell.")]
    [SerializeField] private Image backgroundMask;

    [Tooltip("Foreground Image that displays the MemoryItemData icon.")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Animator controlling 'Formed' and 'Shattered' trigger states.")]
    [SerializeField] private Animator animator;

    // -----------------------------------------------------------------------
    // Trigger Name Constants
    // Rename these to match your Animator trigger names if needed.
    // -----------------------------------------------------------------------

    private const string TRIGGER_FORMED    = "Formed";
    private const string TRIGGER_SHATTERED = "Shattered";

    // -----------------------------------------------------------------------
    // Public API — called by StatsUI
    // -----------------------------------------------------------------------

    /// <summary>
    /// Instantly sets the icon for the immutable core slot (index 0).
    /// Does NOT play any animation — core slots never animate after initial load.
    /// </summary>
    /// <param name="icon">The sprite to display. Pass null to clear.</param>
    public void SetCore(Sprite icon)
    {
        if (itemIcon == null) return;

        if (icon != null)
        {
            itemIcon.sprite  = icon;
            itemIcon.enabled = true;
        }
        else
        {
            itemIcon.sprite  = null;
            itemIcon.enabled = false;
        }
    }

    /// <summary>
    /// Sets the item icon and plays the "Formed" animation (item gained / healed).
    /// </summary>
    /// <param name="icon">The sprite from MemoryItemData.itemIcon.</param>
    public void PlayFormed(Sprite icon)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite  = icon;
            itemIcon.enabled = true;
        }

        if (animator != null)
            animator.SetTrigger(TRIGGER_FORMED);
    }

    /// <summary>
    /// Clears the item icon and plays the "Shattered" animation (item lost / damaged).
    /// </summary>
    public void PlayShattered()
    {
        if (animator != null)
            animator.SetTrigger(TRIGGER_SHATTERED);

        // Clear the sprite after triggering the animation so the Shatter
        // clip can still read the sprite during playback if needed.
        // Swap these two blocks if your Shatter clip should show an empty icon.
        if (itemIcon != null)
        {
            itemIcon.sprite  = null;
            itemIcon.enabled = false;
        }
    }

    /// <summary>
    /// Silently clears the slot with no animation. Useful for initial setup
    /// or resetting without triggering the Shatter clip.
    /// </summary>
    public void InstantlyClear()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite  = null;
            itemIcon.enabled = false;
        }
    }
}
