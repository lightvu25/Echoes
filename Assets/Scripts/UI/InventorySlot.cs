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

    [Header("Playstyle Info (Echo Only)")]
    [SerializeField] private PlaystyleData playstyleData;
    [SerializeField] private Image playstyleIconImage;

    [Header("Cooldown (Tools Only)")]
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMPro.TextMeshProUGUI usesText;

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
        
        if (category == ItemCategory.Echo && playstyleData != null && playstyleIconImage != null)
        {
            playstyleIconImage.sprite = playstyleData.playstyleIcon;
            playstyleIconImage.gameObject.SetActive(playstyleData.playstyleIcon != null);
        }
        else if (playstyleIconImage != null)
        {
            playstyleIconImage.gameObject.SetActive(false);
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (PlayerInventoryCore.Instance != null)
            PlayerInventoryCore.Instance.OnInventoryChanged -= Refresh;
    }
    
    private void Update()
    {
        if (category != ItemCategory.Tool || PlayerInventoryCore.Instance == null) return;
        
        var tool = GetTooltipData() as ToolData;
        if (tool != null && cooldownOverlay != null)
        {
            var playerTool = PlayerInventoryCore.Instance.GetComponent<PlayerTool>();
            if (playerTool != null)
            {
                float remaining = playerTool.GetRemainingCooldown(tool.itemID);
                float total = playerTool.GetTotalCooldown(tool.itemID);
                int uses = playerTool.GetCurrentUses(tool.itemID);
                
                if (remaining > 0f)
                {
                    if (!cooldownOverlay.gameObject.activeSelf) cooldownOverlay.gameObject.SetActive(true);
                    cooldownOverlay.fillAmount = remaining / total;
                }
                else
                {
                    if (cooldownOverlay.gameObject.activeSelf) cooldownOverlay.gameObject.SetActive(false);
                }
                
                if (usesText != null)
                {
                    if (tool.maxUses > 1)
                    {
                        if (!usesText.gameObject.activeSelf) usesText.gameObject.SetActive(true);
                        usesText.text = uses.ToString();
                    }
                    else
                    {
                        if (usesText.gameObject.activeSelf) usesText.gameObject.SetActive(false);
                    }
                }
            }
        }
        else
        {
            if (cooldownOverlay != null && cooldownOverlay.gameObject.activeSelf) cooldownOverlay.gameObject.SetActive(false);
            if (usesText != null && usesText.gameObject.activeSelf) usesText.gameObject.SetActive(false);
        }
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
        if (!PlayerInventoryCore.Instance.IsSlotUnlocked(category, slotIndex)) return null;
        
        IReadOnlyList<ItemBaseData> list = PlayerInventoryCore.Instance.GetEquippedList(category);
        return slotIndex < list.Count ? list[slotIndex] : null;
    }

    private void Refresh()
    {
        if (PlayerInventoryCore.Instance == null) return;

        bool isUnlocked = PlayerInventoryCore.Instance.IsSlotUnlocked(category, slotIndex);

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

    private void OnClicked()
    {
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsInUnlockMode)
        {
            if (PlayerInventoryCore.Instance != null && !PlayerInventoryCore.Instance.IsSlotUnlocked(category, slotIndex))
            {
                InventoryUI.Instance.TryConsumeUnlockPoint(category, slotIndex);
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  Drag and Drop                                                       //
    // ------------------------------------------------------------------ //

    private GameObject ghostIcon;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (PlayerInventoryCore.Instance == null) return;
        
        if (!PlayerInventoryCore.Instance.IsSlotUnlocked(category, slotIndex)) return; // Cannot drag locked slot

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
        
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
        {
            rt.position = worldPoint;
        }

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
            Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                ghostIcon.GetComponent<RectTransform>().position = worldPoint;
            }
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
