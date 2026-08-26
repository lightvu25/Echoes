using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("Run Progress")]
    [Tooltip("Text that displays the total number of defeated enemies.")]
    [SerializeField] private TextMeshProUGUI deadEnemyCountText;
    [Tooltip("Text that displays the player's current evolution tier.")]
    [SerializeField] private TextMeshProUGUI evolutionTierText;

    [Header("Boss / Elite Health Bar")]
    [Tooltip("The complete boss/elite health-bar object. It is shown only while an elite or boss is active.")]
    [SerializeField] private GameObject bossEliteHealthBarRoot;
    [SerializeField] private TextMeshProUGUI bossEliteNameText;
    [SerializeField] private TextMeshProUGUI bossEliteRankText;
    [SerializeField] private TextMeshProUGUI bossEliteHealthText;
    [Tooltip("The foreground Image whose Fill Amount represents the enemy's remaining health.")]
    [SerializeField] private Image bossEliteHealthFill;
    [Tooltip("Optional decorative Image that receives the elite or boss accent colour.")]
    [SerializeField] private Image bossEliteAccent;
    [SerializeField, Min(0.1f)] private float bossEliteHealthFillSpeed = 2.5f;
    [SerializeField] private Color eliteHealthColor = new Color(0.19f, 0.88f, 1f, 1f);
    [SerializeField] private Color bossHealthColor = new Color(1f, 0.24f, 0.22f, 1f);

    // -----------------------------------------------------------------------
    // State Tracking
    // Tracks how many items were active on the PREVIOUS event fire so we can
    // derive the delta (gained / lost) without polling Update().
    // -----------------------------------------------------------------------

    /// <summary>Tracks the previously equipped item per slot index for shatter/form animations.</summary>
    private ItemBaseData[] previousEchoes = new ItemBaseData[10];

    private CanvasGroup canvasGroup;
    private readonly List<EnemyCombat> activeBossEliteEncounters = new List<EnemyCombat>();
    private EvolutionManager evolutionManager;
    private EnemyCombat displayedBossEliteEncounter;
    private float targetBossEliteHealthFill;
    private bool combatHudSubscribed;

    // -----------------------------------------------------------------------
    // IUIPanel Implementation
    // -----------------------------------------------------------------------

    public void Show() { gameObject.SetActive(true); }
    public void Hide() { gameObject.SetActive(false); }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (deadEnemyCountText != null)
            deadEnemyCountText.text = CombatHUDText.FormatDefeatedEnemies(0);
        if (evolutionTierText != null)
            evolutionTierText.text = CombatHUDText.FormatEvolutionTier(null, 0);

        SetBossEliteHealthBarVisible(false);
    }

    private void OnEnable()
    {
        SubscribeCombatHUD();
    }

    private void OnDisable()
    {
        UnsubscribeCombatHUD();
    }

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
        if (evolutionManager == null)
            TryBindEvolutionManager();

        if (displayedBossEliteEncounter == null && activeBossEliteEncounters.Count > 0)
            SelectDisplayedBossEliteEncounter();

        if (displayedBossEliteEncounter != null && bossEliteHealthFill != null)
        {
            bossEliteHealthFill.fillAmount = Mathf.MoveTowards(
                bossEliteHealthFill.fillAmount,
                targetBossEliteHealthFill,
                bossEliteHealthFillSpeed * Time.unscaledDeltaTime);
        }

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
        UnsubscribeCombatHUD();

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

    private void SubscribeCombatHUD()
    {
        if (combatHudSubscribed) return;

        combatHudSubscribed = true;
        EnemyCombat.OnEncounterActivated += HandleBossEliteActivated;
        EnemyCombat.OnEncounterDeactivated += HandleBossEliteDeactivated;

        IReadOnlyList<EnemyCombat> registeredEncounters = EnemyCombat.ActiveEncounters;
        for (int i = 0; i < registeredEncounters.Count; i++)
            AddBossEliteEncounter(registeredEncounters[i]);

        TryBindEvolutionManager();
        SelectDisplayedBossEliteEncounter();
    }

    private void UnsubscribeCombatHUD()
    {
        if (!combatHudSubscribed) return;

        combatHudSubscribed = false;
        EnemyCombat.OnEncounterActivated -= HandleBossEliteActivated;
        EnemyCombat.OnEncounterDeactivated -= HandleBossEliteDeactivated;
        UnbindEvolutionManager();
        BindDisplayedBossEliteEncounter(null);
        activeBossEliteEncounters.Clear();
    }

    private void TryBindEvolutionManager()
    {
        EvolutionManager manager = EvolutionManager.Instance;
        if (manager == null || manager == evolutionManager) return;

        UnbindEvolutionManager();
        evolutionManager = manager;
        evolutionManager.OnKillCountChanged += HandleKillCountChanged;
        evolutionManager.OnTierChanged += HandleEvolutionTierChanged;
        RefreshRunProgress();
    }

    private void UnbindEvolutionManager()
    {
        if (evolutionManager == null) return;

        evolutionManager.OnKillCountChanged -= HandleKillCountChanged;
        evolutionManager.OnTierChanged -= HandleEvolutionTierChanged;
        evolutionManager = null;
    }

    private void HandleKillCountChanged(int _)
    {
        RefreshRunProgress();
    }

    private void HandleEvolutionTierChanged(EvolutionTierData _)
    {
        RefreshRunProgress();
    }

    private void RefreshRunProgress()
    {
        if (evolutionManager == null) return;

        if (deadEnemyCountText != null)
            deadEnemyCountText.text = CombatHUDText.FormatDefeatedEnemies(evolutionManager.CurrentKills);

        if (evolutionTierText != null)
        {
            EvolutionTierData tier = evolutionManager.GetCurrentTierData();
            evolutionTierText.text = CombatHUDText.FormatEvolutionTier(
                tier != null ? tier.tierName : null,
                evolutionManager.CurrentTierIndex);
        }
    }

    private void HandleBossEliteActivated(EnemyCombat encounter)
    {
        AddBossEliteEncounter(encounter);
        SelectDisplayedBossEliteEncounter();
    }

    private void HandleBossEliteDeactivated(EnemyCombat encounter)
    {
        activeBossEliteEncounters.Remove(encounter);
        if (displayedBossEliteEncounter == encounter)
            SelectDisplayedBossEliteEncounter();
    }

    private void AddBossEliteEncounter(EnemyCombat encounter)
    {
        if (encounter == null || !encounter.IsEliteOrBoss || activeBossEliteEncounters.Contains(encounter)) return;
        activeBossEliteEncounters.Add(encounter);
    }

    private void SelectDisplayedBossEliteEncounter()
    {
        for (int i = activeBossEliteEncounters.Count - 1; i >= 0; i--)
        {
            if (activeBossEliteEncounters[i] == null || activeBossEliteEncounters[i].IsDead)
                activeBossEliteEncounters.RemoveAt(i);
        }

        EnemyCombat selected = null;
        for (int i = activeBossEliteEncounters.Count - 1; i >= 0; i--)
        {
            EnemyCombat candidate = activeBossEliteEncounters[i];
            if (selected == null || candidate.Rank > selected.Rank)
                selected = candidate;
        }

        BindDisplayedBossEliteEncounter(selected);
    }

    private void BindDisplayedBossEliteEncounter(EnemyCombat encounter)
    {
        if (displayedBossEliteEncounter != null)
            displayedBossEliteEncounter.OnDamageReceived -= HandleBossEliteDamaged;

        displayedBossEliteEncounter = encounter;

        if (displayedBossEliteEncounter == null)
        {
            SetBossEliteHealthBarVisible(false);
            return;
        }

        displayedBossEliteEncounter.OnDamageReceived += HandleBossEliteDamaged;
        Color accent = displayedBossEliteEncounter.Rank == EnemyRank.Boss
            ? bossHealthColor
            : eliteHealthColor;

        if (bossEliteNameText != null)
            bossEliteNameText.text = CombatHUDText.FormatEncounterName(displayedBossEliteEncounter.gameObject.name);
        if (bossEliteRankText != null)
        {
            bossEliteRankText.text = displayedBossEliteEncounter.Rank == EnemyRank.Boss ? "BOSS" : "ELITE";
            bossEliteRankText.color = accent;
        }
        if (bossEliteAccent != null)
            bossEliteAccent.color = accent;
        if (bossEliteHealthFill != null)
        {
            bossEliteHealthFill.color = accent;
            bossEliteHealthFill.fillAmount = displayedBossEliteEncounter.HPPercent;
        }

        SetBossEliteHealthBarVisible(true);
        RefreshBossEliteHealth();
    }

    private void HandleBossEliteDamaged(object sender, EnemyCombat.DamageReceivedArgs args)
    {
        RefreshBossEliteHealth();
    }

    private void RefreshBossEliteHealth()
    {
        if (displayedBossEliteEncounter == null) return;

        targetBossEliteHealthFill = displayedBossEliteEncounter.HPPercent;
        if (bossEliteHealthText != null)
        {
            bossEliteHealthText.text =
                $"{displayedBossEliteEncounter.CurrentHP:N0} / {displayedBossEliteEncounter.MaxHP:N0}";
        }
    }

    private void SetBossEliteHealthBarVisible(bool visible)
    {
        if (bossEliteHealthBarRoot != null && bossEliteHealthBarRoot.activeSelf != visible)
            bossEliteHealthBarRoot.SetActive(visible);
    }
}

public static class CombatHUDText
{
    public static string FormatDefeatedEnemies(int count)
    {
        return Mathf.Max(0, count).ToString("N0");
    }

    public static string FormatEvolutionTier(string tierName, int tierIndex)
    {
        return Mathf.Max(0, tierIndex).ToString("N0");
    }

    public static string FormatEncounterName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return "UNKNOWN ENCOUNTER";
        return objectName.Replace("(Clone)", string.Empty).Trim().ToUpperInvariant();
    }
}
