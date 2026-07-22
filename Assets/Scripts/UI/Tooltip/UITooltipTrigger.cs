using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Modular hook that attaches to any UI slot.
/// Listens for mouse clicks and passes data to the UITooltipManager.
/// Expects an ITooltipDataProvider on the same GameObject to provide the actual data.
/// </summary>
public class UITooltipTrigger : MonoBehaviour, IPointerClickHandler
{
    private ITooltipDataProvider dataProvider;

    private void Awake()
    {
        // Cache the data provider attached to this slot (e.g., InventorySlot, ShopSlot)
        dataProvider = GetComponent<ITooltipDataProvider>();
        
        if (dataProvider == null)
        {
            Debug.LogWarning($"[UITooltipTrigger] No ITooltipDataProvider found on {gameObject.name}. Tooltips will not trigger.");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dataProvider == null) return;

        // Fetch the dynamic data from the slot
        ItemBaseData data = dataProvider.GetTooltipData();

        // Show data if present, otherwise hide the tooltip
        if (data != null)
        {
            UITooltipManager.Instance?.ShowTooltip(data);
        }
        else
        {
            UITooltipManager.Instance?.HideTooltip();
        }
    }

    private void OnDisable()
    {
        // Failsafe: Hide the tooltip if the UI panel is suddenly closed while hovering
        UITooltipManager.Instance?.HideTooltip();
    }
}
