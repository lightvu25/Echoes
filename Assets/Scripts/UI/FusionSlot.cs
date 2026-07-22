using UnityEngine;
using UnityEngine.EventSystems;

public class FusionSlot : MonoBehaviour, IDropHandler
{
    public EchoData SlottedElement { get; private set; }
    private DraggableElement slottedItemUI;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableElement droppedElement = eventData.pointerDrag.GetComponent<DraggableElement>();
            if (droppedElement != null && droppedElement.elementData != null)
            {
                if (slottedItemUI != null)
                {
                    slottedItemUI.ReturnToInventory();
                }

                droppedElement.transform.SetParent(transform);
                droppedElement.transform.localPosition = Vector3.zero;
                
                SlottedElement = droppedElement.elementData;
                slottedItemUI = droppedElement;

                var ui = UIManager.Instance?.GetPanel<SacrificialUI>(UIPanelType.SacrificialFusion);
                if (ui != null)
                {
                    ui.CheckRecipe();
                }
            }
        }
    }

    public void ClearSlot()
    {
        if (slottedItemUI != null)
        {
            Destroy(slottedItemUI.gameObject);
        }
        SlottedElement = null;
        slottedItemUI = null;
    }

    public void RemoveItemReference()
    {
        SlottedElement = null;
        slottedItemUI = null;
    }
    
    public void ReturnItem()
    {
        if (slottedItemUI != null)
        {
            slottedItemUI.ReturnToInventory();
        }
        SlottedElement = null;
        slottedItemUI = null;
    }
}
