using UnityEngine;
using UnityEngine.EventSystems;

public class FusionSlot : MonoBehaviour, IDropHandler
{
    public EchoData SlottedEcho { get; private set; }
    private DraggableEcho slottedItemUI;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableEcho droppedEcho = eventData.pointerDrag.GetComponent<DraggableEcho>();
            if (droppedEcho != null && droppedEcho.echoData != null)
            {
                if (slottedItemUI != null)
                {
                    slottedItemUI.ReturnToInventory();
                }

                droppedEcho.transform.SetParent(transform);
                droppedEcho.transform.localPosition = Vector3.zero;
                
                SlottedEcho = droppedEcho.echoData;
                slottedItemUI = droppedEcho;

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
        SlottedEcho = null;
        slottedItemUI = null;
    }

    public void RemoveItemReference()
    {
        SlottedEcho = null;
        slottedItemUI = null;
    }
    
    public void ReturnItem()
    {
        if (slottedItemUI != null)
        {
            slottedItemUI.ReturnToInventory();
        }
        SlottedEcho = null;
        slottedItemUI = null;
    }
}
