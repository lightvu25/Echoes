using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, ITooltipDataProvider
{
    [Header("Visuals")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Image glowImage;

    [Header("Identity")]
    [SerializeField] private ItemCategory category;
    [SerializeField] private int slotIndex;

    public ItemCategory Category => category;
    public int SlotIndex => slotIndex;

    [Header("Glow")]
    [SerializeField] private float glowPulseAlpha = 0.8f;
    [SerializeField] private float glowPulseDuration = 0.5f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    private void Start()
    {
        PlayerInventoryCore.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (PlayerInventoryCore.Instance != null)
            PlayerInventoryCore.Instance.OnInventoryChanged -= Refresh;
    }
    
    public void SetGlowing(bool glow)
    {
        if (glowImage == null) return;
        glowImage.DOKill();

        if (glow)
        {
            glowImage.color = new Color(glowImage.color.r, glowImage.color.g, glowImage.color.b, 0f);
            glowImage.gameObject.SetActive(true);
            glowImage.DOFade(glowPulseAlpha, glowPulseDuration)
                     .SetEase(Ease.InOutSine)
                     .SetLoops(-1, LoopType.Yoyo)
                     .SetUpdate(true);
        }
        else
        {
            glowImage.DOFade(0f, glowPulseDuration * 0.5f)
                     .SetUpdate(true)
                     .OnComplete(() => glowImage.gameObject.SetActive(false));
        }
    }

    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    public ItemBaseData GetTooltipData()
    {
        if (PlayerInventoryCore.Instance == null) return null;
        int unlockedCount = GetUnlockedCount();
        if (slotIndex >= unlockedCount) return null;
        
        IReadOnlyList<ItemBaseData> list = PlayerInventoryCore.Instance.GetEquippedList(category);
        return slotIndex < list.Count ? list[slotIndex] : null;
    }

    private void Refresh()
    {
        if (PlayerInventoryCore.Instance == null) return;

        int unlockedCount = GetUnlockedCount();
        bool isUnlocked   = slotIndex < unlockedCount;

        IReadOnlyList<ItemBaseData> list = PlayerInventoryCore.Instance.GetEquippedList(category);
        ItemBaseData item = slotIndex < list.Count ? list[slotIndex] : null;

        // Show locked overlay when not yet unlocked.
        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isUnlocked);

        // Show item icon when occupied.
        if (itemIconImage != null)
        {
            itemIconImage.gameObject.SetActive(item != null && isUnlocked);
            if (item != null && isUnlocked)
            {
                itemIconImage.sprite = item.itemIcon;
                itemIconImage.SetNativeSize();
            }
        }
    }

    private int GetUnlockedCount()
    {
        if (PlayerInventoryCore.Instance == null) return 0;
        return category switch
        {
            ItemCategory.Echo => PlayerInventoryCore.Instance.UnlockedEchoSlots,
            ItemCategory.Relic   => PlayerInventoryCore.Instance.UnlockedRelicSlots,
            _                    => PlayerInventoryCore.Instance.UnlockedEquipmentSlots,
        };
    }

    private void OnClicked()
    {
        // Currently used by SlotUnlockPanel flow only
    }

    // ------------------------------------------------------------------ //
    //  Drag and Drop                                                       //
    // ------------------------------------------------------------------ //

    private GameObject ghostIcon;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (PlayerInventoryCore.Instance == null) return;
        
        int unlockedCount = GetUnlockedCount();
        if (slotIndex >= unlockedCount) return; // Cannot drag locked slot

        IReadOnlyList<ItemBaseData> list = PlayerInventoryCore.Instance.GetEquippedList(category);
        if (slotIndex >= list.Count || list[slotIndex] == null) return; // Cannot drag empty slot

        ghostIcon = new GameObject("DragGhost");
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        ghostIcon.transform.SetParent(rootCanvas.transform, false);

        Image img = ghostIcon.AddComponent<Image>();
        img.sprite = itemIconImage.sprite;
        img.raycastTarget = false; // Important: so it doesn't block OnDrop

        RectTransform rt = ghostIcon.GetComponent<RectTransform>();
        rt.sizeDelta = itemIconImage.rectTransform.rect.size;
        rt.position = eventData.position;

        // Visually fade the original
        if (itemIconImage != null)
        {
            itemIconImage.color = new Color(1, 1, 1, 0.5f);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostIcon != null)
        {
            ghostIcon.GetComponent<RectTransform>().position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostIcon != null)
        {
            Destroy(ghostIcon);
        }
        
        if (itemIconImage != null)
        {
            itemIconImage.color = new Color(1, 1, 1, 1f);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot sourceSlot = eventData.pointerDrag?.GetComponent<InventorySlot>();
        if (sourceSlot != null)
        {
            if (sourceSlot.Category == this.Category)
            {
                PlayerInventoryCore.Instance.MoveOrSwapItem(this.Category, sourceSlot.SlotIndex, this.SlotIndex);
            }
        }
    }
}
