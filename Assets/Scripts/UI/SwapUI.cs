using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SwapUI : MonoBehaviour, IUIPanel
{

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
        panelRoot.localScale = Vector3.zero;
        // Do not disable gameObject here, otherwise Start() will never run!
    }

    private void Start()
    {
        PlayerInventoryCore.Instance.OnSwapRequired += HandleSwapRequired;
        
        // Disable after subscribing
        gameObject.SetActive(false);
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
        
        if (UIManager.Instance != null)
            UIManager.Instance.OpenPanel(UIPanelType.Swap);
        else
            Show();
    }

    // ------------------------------------------------------------------ //
    //  Public API                                                          //
    // ------------------------------------------------------------------ //

    public void Cancel() => Hide();

    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private void BuildSlots(IReadOnlyList<ItemBaseData> equipped)
    {
        // Clear previous buttons.
        foreach (var go in spawnedSlots) Destroy(go);
        spawnedSlots.Clear();

        // Populate incoming item preview.
        if (incomingItemIcon != null) 
        {
            incomingItemIcon.sprite = pendingIncoming.itemIcon;
            incomingItemIcon.preserveAspect = true;
        }
        if (incomingItemName != null) incomingItemName.text   = pendingIncoming.itemName;

        // Spawn one button per currently equipped item.
        foreach (var equippedItem in equipped)
        {
            if (equippedItem == null) continue;

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
        if (UIManager.Instance != null)
            UIManager.Instance.ClosePanelIfOpen(UIPanelType.Swap);
        else
            Hide();
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

    public void Show()
    {
        gameObject.SetActive(true);
        panelRoot.DOKill();
        panelRoot.localScale = Vector3.zero;
        panelRoot.DOScale(1f, popInDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Hide()
    {
        panelRoot.DOKill();
        panelRoot.DOScale(0f, popOutDuration)
                 .SetEase(Ease.InBack)
                 .SetUpdate(true)
                 .OnComplete(() => gameObject.SetActive(false));
    }
}
