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
public class StatsUI : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector Fields — Slots
    // -----------------------------------------------------------------------

    [Header("Core Slot (Index 0 — Immutable)")]
    [SerializeField] private MemorySlot coreSlot;

    [Header("Fragment Slots (Index 1 … N)")]
    [Tooltip("fragmentSlots[i] maps to activeSlots[i + 1] in MemoryInventorySystem.")]
    [SerializeField] private MemorySlot[] fragmentSlots;

    // -----------------------------------------------------------------------
    // Inspector Fields — Currencies
    // -----------------------------------------------------------------------

    [Header("Currencies")]
    [SerializeField] private TextMeshProUGUI coinsTextMesh;
    [SerializeField] private TextMeshProUGUI memsTextMesh;

    // -----------------------------------------------------------------------
    // State Tracking
    // Tracks how many items were active on the PREVIOUS event fire so we can
    // derive the delta (gained / lost) without polling Update().
    // -----------------------------------------------------------------------

    /// <summary>Number of active slots (including core) from the last event.</summary>
    private int previousActiveCount = 0;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    private void Start()
    {
        // --- Currency events (PlayerStats) ---
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnGoldChanged            += UpdateCoins;
            PlayerStats.Instance.OnMemoryFragmentsChanged += UpdateMems;

            UpdateCoins(PlayerStats.Instance.CurrentGold);
            UpdateMems(PlayerStats.Instance.MemoryFragments);
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

        if (MemoryInventorySystem.Instance != null)
        {
            MemoryInventorySystem.Instance.OnInventoryChanged += UpdateHealthSlots;

            UpdateHealthSlots(MemoryInventorySystem.Instance.activeSlots);
        }
        else
        {
            Debug.LogWarning("StatsUI: MemoryInventorySystem.Instance is null in Start(). " +
                             "Make sure the Player object is present and its Awake() runs first.");
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnGoldChanged            -= UpdateCoins;
            PlayerStats.Instance.OnMemoryFragmentsChanged -= UpdateMems;
        }

        if (PlayerStats.Instance != null)
        {
            HealthSystem hs = PlayerStats.Instance.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.OnSlotsChanged -= HandleSlotsChanged;
            }
        }

        if (MemoryInventorySystem.Instance != null)
        {
            MemoryInventorySystem.Instance.OnInventoryChanged -= UpdateHealthSlots;
        }
    }
    
    private void UpdateHealthSlots(IReadOnlyList<MemoryItemData> activeSlots)
    {
        int currentCount = activeSlots.Count;

        // --- Core slot (activeSlots[0]) ---
        // Core never shatters; it is set silently whenever there is at least one item.
        if (coreSlot != null && currentCount > 0)
        {
            coreSlot.SetCore(activeSlots[0].itemIcon);
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
            if (fragmentSlots[i] != null && fragmentSlots[i].gameObject.activeSelf != shouldBeActive)
            {
                fragmentSlots[i].gameObject.SetActive(shouldBeActive);
            }
        }
    }

    private void UpdateCoins(int amount)
    {
        if (coinsTextMesh != null)
            coinsTextMesh.text = amount.ToString();
    }

    private void UpdateMems(int amount)
    {
        if (memsTextMesh != null)
            memsTextMesh.text = amount.ToString();
    }

    /* 
    ========================================================================
    ARCHITECTURAL NOTE: SKILLS UI (Upcoming Feature)
    ========================================================================
    When implementing the Skills UI, do NOT poll in Update().
    Create a new `SkillUIController` and subscribe to events, e.g.:
        PlayerSkills.Instance.OnSkillCooldownChanged += UpdateSkillCooldownOverlay;
    Always unsubscribe in OnDestroy() to prevent memory leaks.
    ======================================================================== 
    */
}