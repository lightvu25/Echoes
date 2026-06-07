using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single slot button inside the <see cref="SwapUI"/> inline overlay.
/// Displays the icon and name of an equipped item and invokes a callback when clicked.
/// </summary>
[RequireComponent(typeof(Button))]
public class SwapSlotButton : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMPro.TextMeshProUGUI itemLabel;

    private Button button;

    private void Awake() => button = GetComponent<Button>();

    /// <summary>
    /// Configures the button visuals and wires the click callback.
    /// </summary>
    /// <param name="item">The equipped item this button represents.</param>
    /// <param name="onClicked">Action invoked when the player clicks this slot.</param>
    public void Setup(ItemBaseData item, Action onClicked)
    {
        if (item == null) return;

        if (itemIcon  != null) itemIcon.sprite = item.itemIcon;
        if (itemLabel != null) itemLabel.text   = item.itemName;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked?.Invoke());
    }
}
