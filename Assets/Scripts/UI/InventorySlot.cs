using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class InventorySlot : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Image glowImage;

    [Header("Identity")]
    [SerializeField] private ItemCategory category;
    [SerializeField] private int slotIndex;

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
                itemIconImage.sprite = item.itemIcon;
        }
    }

    private int GetUnlockedCount()
    {
        if (PlayerInventoryCore.Instance == null) return 0;
        return category switch
        {
            ItemCategory.Echo => PlayerInventoryCore.Instance.UnlockedEchoSlots,
            ItemCategory.Relic   => PlayerInventoryCore.Instance.UnlockedRelicSlots,
            _                    => PlayerInventoryCore.Instance.UnlockedItemSlots,
        };
    }

    private void OnClicked()
    {
        // Currently used by SlotUnlockPanel flow only — InventoryUI handles the callback.
        // Future: allow drag-and-drop or right-click drop from here.
    }
}
