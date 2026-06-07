using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Inline swap overlay that appears above the player when they try to equip
/// an item into a full inventory category.
///
/// Visual design: a small non-modal popup anchored to the player's screen position,
/// animated with a DOTween pop-in (scale 0 → 1 + slight overshoot).
///
/// Decoupling note
/// ---------------
/// This class has ZERO game logic. It translates UI clicks into calls to
/// <see cref="PlayerInventoryCore.SwapItem"/> and then dismisses itself.
/// </summary>
public class SwapUI : MonoBehaviour
{
    public static SwapUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotButtonPrefab;
    [SerializeField] private Image incomingItemIcon;
    [SerializeField] private TMPro.TextMeshProUGUI incomingItemName;

    [Header("Animation")]
    [SerializeField] private float popInDuration  = 0.2f;
    [SerializeField] private float popOutDuration = 0.15f;

    [Header("World Anchor Offset (screen-space)")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 120f);

    private ItemBaseData pendingIncoming;
    private List<GameObject> spawnedSlots = new();

    // ------------------------------------------------------------------ //
    //  Unity lifecycle                                                     //
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        panelRoot.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        PlayerInventoryCore.Instance.OnSwapRequired += HandleSwapRequired;
    }

    private void OnDestroy()
    {
        if (PlayerInventoryCore.Instance != null)
            PlayerInventoryCore.Instance.OnSwapRequired -= HandleSwapRequired;
    }

    // ------------------------------------------------------------------ //
    //  Event handler                                                       //
    // ------------------------------------------------------------------ //

    private void HandleSwapRequired(ItemBaseData incoming, ItemCategory category)
    {
        pendingIncoming = incoming;
        BuildSlots(PlayerInventoryCore.Instance.GetEquippedList(category));
        PositionNearPlayer();
        PopIn();
    }

    // ------------------------------------------------------------------ //
    //  Public API                                                          //
    // ------------------------------------------------------------------ //

    /// <summary>Dismisses the swap overlay without making any changes.</summary>
    public void Cancel() => PopOut();

    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private void BuildSlots(IReadOnlyList<ItemBaseData> equipped)
    {
        // Clear previous buttons.
        foreach (var go in spawnedSlots) Destroy(go);
        spawnedSlots.Clear();

        // Populate incoming item preview.
        if (incomingItemIcon != null) incomingItemIcon.sprite = pendingIncoming.itemIcon;
        if (incomingItemName != null) incomingItemName.text   = pendingIncoming.itemName;

        // Spawn one button per currently equipped item.
        foreach (var equippedItem in equipped)
        {
            ItemBaseData captured = equippedItem; // capture for lambda

            GameObject slotGO = Instantiate(slotButtonPrefab, slotContainer);
            spawnedSlots.Add(slotGO);

            // Wire up icon + name if the prefab has them.
            if (slotGO.TryGetComponent(out SwapSlotButton slotBtn))
            {
                slotBtn.Setup(captured, () => ConfirmSwap(captured));
            }
            else if (slotGO.TryGetComponent(out Button btn))
            {
                btn.onClick.AddListener(() => ConfirmSwap(captured));
            }
        }
    }

    private void ConfirmSwap(ItemBaseData equipped)
    {
        PlayerInventoryCore.Instance.SwapItem(equipped, pendingIncoming);
        pendingIncoming = null;
        PopOut();
    }

    private void PositionNearPlayer()
    {
        if (PlayerStats.Instance == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos   = PlayerStats.Instance.transform.position;
        Vector3 screenPos  = cam.WorldToScreenPoint(worldPos);
        panelRoot.position = screenPos + (Vector3)screenOffset;
    }

    private void PopIn()
    {
        gameObject.SetActive(true);
        panelRoot.DOKill();
        panelRoot.localScale = Vector3.zero;
        panelRoot.DOScale(1f, popInDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void PopOut()
    {
        panelRoot.DOKill();
        panelRoot.DOScale(0f, popOutDuration)
                 .SetEase(Ease.InBack)
                 .SetUpdate(true)
                 .OnComplete(() => gameObject.SetActive(false));
    }
}
