using System;
using UnityEngine;

/// <summary>
/// Singleton manager that drives in-run enemy evolution.
/// Tracks kill count, advances evolution tiers, and broadcasts
/// tier-change and global-aggro events to all EnemyEvolutionModifier listeners.
/// </summary>
public class EvolutionManager : MonoBehaviour
{
    public static EvolutionManager Instance { get; private set; }

    /// <summary>Fired when the evolution tier advances. Carries the new tier data.</summary>
    public event Action<EvolutionTierData> OnTierChanged;

    /// <summary>Fired when any enemy spots the player (shared vision propagation).</summary>
    public event Action OnGlobalAggroTriggered;

    [Header("Tier Configuration")]
    [Tooltip("Ordered list of evolution tiers. Index 0 is the base tier (no evolution).")]
    [SerializeField] private EvolutionTierData[] tiers;

    [Header("Global Aggro")]
    [Tooltip("Radius within which enemies receive the shared aggro signal. Use Infinity for all enemies.")]
    [SerializeField] private float globalAggroRadius = Mathf.Infinity;

    private int currentKills;
    private int currentTierIndex;
    private bool isLocked; // locked by Forgotten_Hourglass relic

    /// <summary>Current kill count this run.</summary>
    public int CurrentKills => currentKills;

    /// <summary>Currently active evolution tier index.</summary>
    public int CurrentTierIndex => currentTierIndex;

    /// <summary>Radius used for shared vision propagation.</summary>
    public float GlobalAggroRadius => globalAggroRadius;

    /// <summary>Returns the currently active EvolutionTierData, or null if no tiers are configured.</summary>
    public EvolutionTierData GetCurrentTierData()
    {
        if (tiers == null || tiers.Length == 0) return null;
        return tiers[Mathf.Clamp(currentTierIndex, 0, tiers.Length - 1)];
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Initializes the evolution state for the current run.
    /// Checks for the Forgotten_Hourglass relic and sets the starting tier.
    /// </summary>
    private void Initialize()
    {
        currentKills = 0;
        currentTierIndex = 0;
        isLocked = false;

        // Check if the player has the Forgotten_Hourglass relic — locks evolution at tier 0
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null
            && player.TryGetComponent<InventoryManager>(out var inv)
            && inv.HasRelic("Forgotten_Hourglass"))
        {
            isLocked = true;
        }

        // Fire initial tier so any already-spawned enemies pick up tier 0 data
        EvolutionTierData initialTier = GetCurrentTierData();
        if (initialTier != null)
        {
            OnTierChanged?.Invoke(initialTier);
        }
    }

    /// <summary>
    /// Called when an enemy is killed. Increments the kill counter and
    /// checks whether the next evolution tier threshold has been reached.
    /// </summary>
    public void RegisterKill()
    {
        currentKills++;

        if (isLocked) return; // Forgotten_Hourglass prevents tier advancement

        if (tiers == null || tiers.Length == 0) return;

        // Check if the next tier's kill threshold has been met
        int nextTierIndex = currentTierIndex + 1;
        if (nextTierIndex < tiers.Length)
        {
            if (currentKills >= tiers[nextTierIndex].requiredKills)
            {
                currentTierIndex = nextTierIndex;
                EvolutionTierData newTier = tiers[currentTierIndex];
                
                Debug.Log($"<color=cyan>[Evolution] Tier Advanced! Now at: {newTier.tierName} (Kills: {currentKills})</color>");
                OnTierChanged?.Invoke(newTier);
            }
        }
    }

    /// <summary>
    /// Called by an enemy when it spots the player.
    /// Propagates global aggro to all subscribed enemies.
    /// The EnemyEvolutionModifier on each enemy handles radius filtering.
    /// </summary>
    public void ReportPlayerSpotted(Vector3 spotterPosition)
    {
        OnGlobalAggroTriggered?.Invoke();
    }
}
