using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public EchoData elementData { get; private set; }
    public Transform originalParent { get; private set; }
    public Transform defaultParent { get; private set; }

    private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (iconImage == null) iconImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (defaultParent == null) defaultParent = transform.parent;
    }

    public void Setup(EchoData data)
    {
        elementData = data;
        if (iconImage != null)
        {
            if (data != null)
            {
                iconImage.sprite = data.itemIcon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (elementData == null) return;

        originalParent = transform.parent;

        // If dragging out of a FusionSlot, clear the slot's reference
        FusionSlot slot = originalParent.GetComponent<FusionSlot>();
        if (slot != null)
        {
            slot.RemoveItemReference();
            var ui = UIManager.Instance?.GetPanel<SacrificialUI>(UIPanelType.SacrificialFusion);
            if (ui != null) ui.CheckRecipe();
        }

        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (elementData == null) return;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (elementData == null) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (transform.parent == transform.root)
        {
            ReturnToInventory();
        }
    }

    public void ReturnToOriginalParent()
    {
        transform.SetParent(originalParent);
        transform.localPosition = Vector3.zero;
    }

    public void ReturnToInventory()
    {
        if (defaultParent != null)
        {
            transform.SetParent(defaultParent);
            transform.localPosition = Vector3.zero;
        }
        else
        {
            ReturnToOriginalParent();
        }
    }
}
