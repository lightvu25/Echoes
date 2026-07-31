using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manager for the Health-as-Inventory HUD.
/// Responsibilities:
///   • Subscribe to inventory and currency events.
///   • Calculate the COUNT DELTA between the previous and current state.
///   • Delegate animation/sprite commands to individual <see cref="MemorySlot"/> components.
///
/// This class intentionally contains NO animation or sprite logic; that belongs in MemorySlot.
/// </summary>
public class StatsUI : MonoBehaviour, IUIPanel
{
    // -----------------------------------------------------------------------
    // Inspector Fields — Slots
    // -----------------------------------------------------------------------

    [Header("Echo Slots (Health Shards)")]
    [Tooltip("echoSlots[i] maps to active Echoes in Inventory. Index 0 is the Core.")]
    [SerializeField] private MemorySlot[] echoSlots;

    [Header("Equipment Slots")]
    [SerializeField] private MemorySlot[] toolSlots;

    [Header("Playstyle Icons")]
    [Tooltip("Maps to slots: 0=Melee, 1=MidRange, 2=LongRange, 3=Magic.")]
    [SerializeField] private PlaystyleData[] slotPlaystyles;

    // -----------------------------------------------------------------------
    // Inspector Fields — Currencies
    // -----------------------------------------------------------------------

    [Header("Currencies")]
    [SerializeField] private TextMeshProUGUI coinsTextMesh;
    [SerializeField] private TextMeshProUGUI astralShardsTextMesh;

    [Header("Crimson Amber")]
    [SerializeField] private CrimsonAmberUI crimsonAmberUI;

    [Header("Low Health Visuals")]
    [SerializeField] private CanvasGroup lowHealthOverlay;
    [SerializeField] private float lowHealthThreshold = 0.35f;
    [SerializeField] private float pulseSpeed = 2f;

    // -----------------------------------------------------------------------
    // State Tracking
    // Tracks how many items were active on the PREVIOUS event fire so we can
    // derive the delta (gained / lost) without polling Update().
    // -----------------------------------------------------------------------

    /// <summary>Tracks the previously equipped item per slot index for shatter/form animations.</summary>
    private ItemBaseData[] previousEchoes = new ItemBaseData[10];

    private CanvasGroup canvasGroup;

    // -----------------------------------------------------------------------
    // IUIPanel Implementation
    // -----------------------------------------------------------------------

    public void Show() { gameObject.SetActive(true); }
    public void Hide() { gameObject.SetActive(false); }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        InventoryUI.OnInventoryToggled += HandleInventoryToggled;

        // Clear editor mockups
        if (echoSlots != null)
        {
            foreach (var slot in echoSlots)
            {
                if (slot != null) slot.InstantlyClear();
            }
        }

        // --- Initialize playstyle background icons ---
        if (slotPlaystyles != null && echoSlots != null)
        {
            for (int i = 0; i < echoSlots.Length; i++)
            {
                if (echoSlots[i] != null && slotPlaystyles.Length > i && slotPlaystyles[i] != null)
                {
                    echoSlots[i].SetPlaystyleIcon(slotPlaystyles[i].playstyleIcon);
                }
            }
        }

        // --- Currency events (PlayerStats) ---
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnGoldChanged += UpdateCoins;
            PlayerStats.Instance.OnAstralShardsChanged += UpdateAstralShards;

            UpdateCoins(PlayerStats.Instance.CurrentGold);
            UpdateAstralShards(PlayerStats.Instance.CurrentAstralShards);
        }

        // HealthSystem.OnSlotsChanged is no longer used for Echo slots (they are uncoupled).

        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged += HandleInventoryChanged;

            HandleInventoryChanged();
        }
        else
        {
            Debug.LogWarning("StatsUI: PlayerInventoryCore.Instance is null in Start(). " +
                             "Make sure the Player object is present and its Awake() runs first.");
        }

        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.OnDead += HandlePlayerDead;
        }

        if (PlayerStats.Instance != null)
        {
            var amberController = PlayerStats.Instance.GetComponent<CrimsonAmber>();
            if (amberController != null)
            {
                amberController.OnAmberStateChanged += UpdateCrimsonAmberUI;
                UpdateCrimsonAmberUI(amberController.CurrentAmbers, amberController.MaxAmbers, amberController.CurrentOrbs);
            }
        }
    }

    private void Update()
    {
        if (PlayerInventoryCore.Instance != null && lowHealthOverlay != null)
        {
            HealthSystem hs = PlayerInventoryCore.Instance.GetComponent<HealthSystem>();
            if (hs != null)
            {
                if (hs.HPPercent <= lowHealthThreshold && hs.CurrentHP > 0 && !hs.IsDead)
                {
                    if (!lowHealthOverlay.gameObject.activeSelf)
                        lowHealthOverlay.gameObject.SetActive(true);
                        
                    // Pulse alpha between 0.3 and 0.9
                    lowHealthOverlay.alpha = 0.3f + Mathf.PingPong(Time.time * pulseSpeed, 0.6f);
                }
                else
                {
                    if (lowHealthOverlay.gameObject.activeSelf)
                    {
                        lowHealthOverlay.alpha = 0f;
                        lowHealthOverlay.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        InventoryUI.OnInventoryToggled -= HandleInventoryToggled;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnGoldChanged -= UpdateCoins;
            PlayerStats.Instance.OnAstralShardsChanged -= UpdateAstralShards;
        }

        // HealthSystem.OnSlotsChanged removed

        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.OnDead -= HandlePlayerDead;
        }

        if (PlayerStats.Instance != null)
        {
            var amberController = PlayerStats.Instance.GetComponent<CrimsonAmber>();
            if (amberController != null)
            {
                amberController.OnAmberStateChanged -= UpdateCrimsonAmberUI;
            }
        }
    }

    private void HandleInventoryToggled(bool isOpen)
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            float targetAlpha = isOpen ? 0.2f : 1f;
            canvasGroup.DOFade(targetAlpha, 0.3f).SetUpdate(true);
        }
    }

    private void HandlePlayerDead(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void HandleInventoryChanged()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            UpdateHealthSlots(PlayerInventoryCore.Instance.EquippedEchoes);

            var activeTools = new System.Collections.Generic.List<ItemBaseData>();
            foreach (var item in PlayerInventoryCore.Instance.EquippedTools)
            {
                if (item != null) activeTools.Add(item);
            }
            UpdateEquipmentSlots(toolSlots, activeTools, PlayerInventoryCore.Instance.UnlockedEquipmentSlots);
        }
    }

    private void UpdateEquipmentSlots(MemorySlot[] slots, IReadOnlyList<ItemBaseData> activeItems, int unlockedSlots)
    {
        if (slots == null) return;
        
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            
            bool shouldBeActive = i < unlockedSlots;

            if (shouldBeActive && !slots[i].gameObject.activeSelf)
            {
                slots[i].gameObject.SetActive(true);
            }
            else if (!shouldBeActive && slots[i].gameObject.activeSelf)
            {
                slots[i].gameObject.SetActive(false);
            }

            if (shouldBeActive)
            {
                if (i < activeItems.Count)
                {
                    slots[i].SetCore(activeItems[i].itemIcon);
                }
                else
                {
                    slots[i].InstantlyClear();
                }
            }
        }
    }

    private void UpdateHealthSlots(IReadOnlyList<ItemBaseData> equippedEchoes)
    {
        if (echoSlots == null) return;
        
        for (int i = 0; i < echoSlots.Length; i++)
        {
            if (echoSlots[i] == null) continue;

            bool isUnlocked = PlayerInventoryCore.Instance.IsSlotUnlocked(ItemCategory.Echo, i);
            ItemBaseData currentItem = i < equippedEchoes.Count ? equippedEchoes[i] : null;
            ItemBaseData prevItem = i < previousEchoes.Length ? previousEchoes[i] : null;

            // 1. Handle Shell Visibility
            if (isUnlocked && !echoSlots[i].gameObject.activeSelf)
            {
                echoSlots[i].gameObject.SetActive(true);
                if (i > 0) echoSlots[i].PlayShellFormed(); 
            }
            else if (!isUnlocked && echoSlots[i].gameObject.activeSelf)
            {
                echoSlots[i].gameObject.SetActive(false);
            }

            // 2. Handle Core/Shatter Animations
            if (isUnlocked)
            {
                if (currentItem != null && prevItem == null)
                {
                    // Item equipped
                    if (i == 0) echoSlots[i].SetCore(currentItem.itemIcon);
                    else echoSlots[i].PlayFormed(currentItem.itemIcon);
                }
                else if (currentItem == null && prevItem != null)
                {
                    // Item lost (shattered)
                    if (i == 0) echoSlots[i].InstantlyClear();
                    else echoSlots[i].PlayShattered();
                }
                else if (currentItem != null && prevItem != null && currentItem != prevItem)
                {
                    // Item swapped
                    echoSlots[i].SetCore(currentItem.itemIcon);
                }
                else if (currentItem != null && prevItem == currentItem)
                {
                    // Ensure it stays set on full refresh
                    echoSlots[i].SetCore(currentItem.itemIcon);
                }
            }

            // Save state
            if (i < previousEchoes.Length) previousEchoes[i] = currentItem;
        }
    }

    private void UpdateCoins(int amount)
    {
        if (coinsTextMesh != null)
            coinsTextMesh.text = amount.ToString();
    }

    private void UpdateAstralShards(int amount)
    {
        if (astralShardsTextMesh != null)
            astralShardsTextMesh.text = amount.ToString();
    }

    private void UpdateCrimsonAmberUI(int currentAmbers, int maxAmbers, int currentOrbs)
    {
        if (crimsonAmberUI != null)
        {
            crimsonAmberUI.UpdateVisuals(currentAmbers, maxAmbers, currentOrbs);
        }
    }
}