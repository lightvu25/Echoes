using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    [Header("Core Slot (Index 0 — Immutable)")]
    [SerializeField] private MemorySlot coreSlot;

    [Header("Fragment Slots (Index 1 … N)")]
    [Tooltip("fragmentSlots[i] maps to activeSlots[i + 1] in Inventory.")]
    [SerializeField] private MemorySlot[] fragmentSlots;

    // -----------------------------------------------------------------------
    // Inspector Fields — Currencies
    // -----------------------------------------------------------------------

    [Header("Currencies")]
    [SerializeField] private TextMeshProUGUI coinsTextMesh;
    [SerializeField] private TextMeshProUGUI astralShardsTextMesh;

    [Header("Crimson Amber")]
    [SerializeField] private CrimsonAmberUI crimsonAmberUI;

    // -----------------------------------------------------------------------
    // State Tracking
    // Tracks how many items were active on the PREVIOUS event fire so we can
    // derive the delta (gained / lost) without polling Update().
    // -----------------------------------------------------------------------

    /// <summary>Number of active slots (including core) from the last event.</summary>
    private int previousActiveCount = 0;

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
        // Clear editor mockups
        if (coreSlot != null) coreSlot.InstantlyClear();
        if (fragmentSlots != null)
        {
            foreach (var slot in fragmentSlots)
            {
                if (slot != null) slot.InstantlyClear();
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

        if (PlayerStats.Instance != null)
        {
            HealthSystem hs = PlayerStats.Instance.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.OnSlotsChanged += HandleSlotsChanged;
                HandleSlotsChanged(hs.UnlockedSlots);
            }
        }

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

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnGoldChanged -= UpdateCoins;
            PlayerStats.Instance.OnAstralShardsChanged -= UpdateAstralShards;
        }

        if (PlayerStats.Instance != null)
        {
            HealthSystem hs = PlayerStats.Instance.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.OnSlotsChanged -= HandleSlotsChanged;
            }
        }

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

    private void HandlePlayerDead(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void HandleInventoryChanged()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            var activeItems = new System.Collections.Generic.List<ItemBaseData>();
            foreach (var item in PlayerInventoryCore.Instance.EquippedEchoes)
            {
                if (item != null) activeItems.Add(item);
            }
            UpdateHealthSlots(activeItems);
        }
    }

    private void UpdateHealthSlots(IReadOnlyList<ItemBaseData> activeSlots)
    {
        int currentCount = activeSlots.Count;

        // --- Core slot (activeSlots[0]) ---
        // Core never shatters; it is set silently whenever there is at least one item.
        if (coreSlot != null)
        {
            if (currentCount > 0)
            {
                coreSlot.SetCore(activeSlots[0].itemIcon);
            }
            else
            {
                coreSlot.InstantlyClear();
            }
        }

        if (fragmentSlots != null)
        {
            if (currentCount < previousActiveCount)
            {
                for (int i = currentCount; i < previousActiveCount; i++)
                {
                    int fragmentIndex = i - 1;
                    if (fragmentIndex >= 0 && fragmentIndex < fragmentSlots.Length)
                        fragmentSlots[fragmentIndex].PlayShattered();
                }
            }
            else if (currentCount > previousActiveCount)
            {
                for (int i = previousActiveCount; i < currentCount; i++)
                {
                    int fragmentIndex = i - 1;
                    if (fragmentIndex >= 0 && fragmentIndex < fragmentSlots.Length)
                        fragmentSlots[fragmentIndex].PlayFormed(activeSlots[i].itemIcon);
                }
            }
        }

        previousActiveCount = currentCount;
    }

    private void HandleSlotsChanged(int unlockedSlots)
    {
        if (fragmentSlots == null) return;

        int maxFragments = Mathf.Max(0, unlockedSlots - 1); // core is always 1, fragments are the rest
        for (int i = 0; i < fragmentSlots.Length; i++)
        {
            bool shouldBeActive = i < maxFragments;
            if (fragmentSlots[i] != null)
            {
                if (shouldBeActive && !fragmentSlots[i].gameObject.activeSelf)
                {
                    fragmentSlots[i].gameObject.SetActive(true);
                    fragmentSlots[i].PlayShellFormed(); // Play the form animation when recovering!
                }
                else if (!shouldBeActive && fragmentSlots[i].gameObject.activeSelf)
                {
                    fragmentSlots[i].gameObject.SetActive(false);
                }
            }
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