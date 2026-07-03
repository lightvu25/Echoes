using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Displays three "next lockable slot" buttons — one per inventory category.
/// This panel is shown during "Slot Unlock Mode" (triggered by gaining Max HP).
///
/// It is completely stateless: slot counts are pushed in via <see cref="Display"/>.
/// Clicking a button calls back into <see cref="InventoryUI.OnSlotChosen"/>.
/// </summary>
public class SlotUnlockPanel : MonoBehaviour
{
    [Header("Category Buttons")]
    [SerializeField] private Button elementButton;
    [SerializeField] private Button relicButton;
    [SerializeField] private Button itemButton;

    [Header("Slot Labels (optional)")]
    [SerializeField] private TMPro.TextMeshProUGUI elementLabel;
    [SerializeField] private TMPro.TextMeshProUGUI relicLabel;
    [SerializeField] private TMPro.TextMeshProUGUI itemLabel;

    [Header("Glow Animation")]
    [SerializeField] private float glowPulseScale = 1.08f;
    [SerializeField] private float glowPulseDuration = 0.6f;

    // ------------------------------------------------------------------ //
    //  Unity lifecycle                                                     //
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        elementButton?.onClick.AddListener(() => Choose(ItemCategory.Element));
        relicButton  ?.onClick.AddListener(() => Choose(ItemCategory.Relic));
        itemButton   ?.onClick.AddListener(() => Choose(ItemCategory.Item));

        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ //
    //  Public API                                                          //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Shows the panel and updates button states to reflect current slot counts.
    /// Buttons for maxed-out categories are hidden.
    /// </summary>
    /// <param name="elementSlots">Current unlocked Element slot count.</param>
    /// <param name="relicSlots">Current unlocked Relic slot count.</param>
    /// <param name="itemSlots">Current unlocked Item slot count.</param>
    public void Display(int elementSlots, int relicSlots, int itemSlots)
    {
        gameObject.SetActive(true);

        RefreshButton(elementButton, elementLabel, elementSlots, "Element", ItemCategory.Element);
        RefreshButton(relicButton,   relicLabel,   relicSlots,   "Relic",   ItemCategory.Relic);
        RefreshButton(itemButton,    itemLabel,     itemSlots,    "Item",    ItemCategory.Item);

        PlayGlowPulse();
    }

    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private void RefreshButton(
        Button button, TMPro.TextMeshProUGUI label,
        int current, string categoryName, ItemCategory category)
    {
        if (button == null) return;

        bool canUnlock = current < RunData.MAX_SLOTS;
        button.gameObject.SetActive(canUnlock);

        if (label != null && canUnlock)
            label.text = $"Unlock {categoryName} Slot\n({current}/{RunData.MAX_SLOTS})";
    }

    private void Choose(ItemCategory category)
    {
        gameObject.SetActive(false);
        UIManager.Instance?.GetPanel<InventoryUI>(UIPanelType.Inventory)?.OnSlotChosen(category);
    }

    private void PlayGlowPulse()
    {
        // Pulse each visible button with a DOTween scale loop.
        PulseButton(elementButton);
        PulseButton(relicButton);
        PulseButton(itemButton);
    }

    private void PulseButton(Button button)
    {
        if (button == null || !button.gameObject.activeSelf) return;

        button.transform
              .DOScale(glowPulseScale, glowPulseDuration)
              .SetEase(Ease.InOutSine)
              .SetLoops(-1, LoopType.Yoyo)
              .SetUpdate(true); // runs while timeScale == 0
    }
}
