using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Global Item Pools")]
    [SerializeField] private List<ToolData> masterTools;
    [SerializeField] private List<RelicData> masterRelics;
    [SerializeField] private List<EchoData> masterEchoes;

    private List<ToolData> availableTools;
    private List<RelicData> availableRelics;
    private List<EchoData> availableEchoes;

    [System.Serializable]
    public struct TierChance
    {
        public int tier;
        [Range(0, 100)] public float chance;
    }

    [Header("Relic Tier Chances")]
    [Tooltip("Setup the percentage for each tier. Make sure they sum up to 100. Example: Tier 1 = 60, Tier 2 = 30, Tier 3 = 10")]
    [SerializeField] private List<TierChance> relicTierChances = new List<TierChance>
    {
        new TierChance { tier = 1, chance = 60f },
        new TierChance { tier = 2, chance = 30f },
        new TierChance { tier = 3, chance = 10f }
    };

    [Header("Echo Tier Chances")]
    [Tooltip("Setup the percentage for each tier. Make sure they sum up to 100.")]
    [SerializeField] private List<TierChance> echoTierChances = new List<TierChance>
    {
        new TierChance { tier = 1, chance = 60f },
        new TierChance { tier = 2, chance = 30f },
        new TierChance { tier = 3, chance = 10f }
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
            // DontDestroyOnLoad(gameObject); // Uncomment if this needs to persist across scene loads
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        availableTools = new List<ToolData>(masterTools);
        availableRelics = new List<RelicData>(masterRelics);
        availableEchoes = new List<EchoData>(masterEchoes);
    }

    /// <summary>
    /// Pulls a random item from the global pool.
    /// Relics and Echoes are removed from the pool so they cannot be encountered twice in the same run.
    /// </summary>
    public ItemBaseData GetRandomItem(ItemCategory category, int minTier = 1)
    {
        switch (category)
        {
            case ItemCategory.Tool: // Tools
                return GetAndRemoveRandom(availableTools.Cast<ItemBaseData>().ToList(), minTier, false, null);
            case ItemCategory.Relic:
                return GetAndRemoveRandom(availableRelics.Cast<ItemBaseData>().ToList(), minTier, true, relicTierChances);
            case ItemCategory.Echo:
                return GetAndRemoveRandom(availableEchoes.Cast<ItemBaseData>().ToList(), minTier, true, echoTierChances);
            default:
                return null;
        }
    }

    private ItemBaseData GetAndRemoveRandom(List<ItemBaseData> pool, int minTier, bool removeFromPool, List<TierChance> tierChances)
    {
        // First, filter by minTier
        var validItems = pool.Where(item => 
        {
            if (item == null) return false;
            return GetItemTier(item) >= minTier;
        }).ToList();

        if (validItems.Count == 0) 
        {
            Debug.LogWarning($"[ShopManager] No valid items found for minTier {minTier}!");
            return null;
        }

        ItemBaseData selected = null;

        // If we have custom tier chances, try to pick an item of a specific tier
        if (tierChances != null && tierChances.Count > 0)
        {
            var availableTiers = validItems.Select(item => GetItemTier(item)).Distinct().ToList();
            var validChances = tierChances.Where(c => availableTiers.Contains(c.tier)).ToList();

            if (validChances.Count > 0)
            {
                int targetTier = RollForTier(validChances);
                
                // Try to find items of exactly the target tier
                var tierSpecificItems = validItems.Where(item => GetItemTier(item) == targetTier).ToList();
                
                if (tierSpecificItems.Count > 0)
                {
                    selected = tierSpecificItems[Random.Range(0, tierSpecificItems.Count)];
                }
            }
        }

        // Fallback to random if no tier chances were provided, or if the target tier had no valid items left
        if (selected == null)
        {
            selected = validItems[Random.Range(0, validItems.Count)];
        }
        
        if (removeFromPool)
        {
            if (selected is RelicData r) availableRelics.Remove(r);
            if (selected is EchoData e) availableEchoes.Remove(e);
        }

        return selected;
    }

    private int GetItemTier(ItemBaseData item)
    {
        if (item is RelicData relic) return (int)relic.Rarity + 1;
        if (item is EchoData echo) return echo.level;
        return item.itemTier;
    }

    private int RollForTier(List<TierChance> chances)
    {
        float total = chances.Sum(c => c.chance);
        float roll = Random.Range(0, total);
        float current = 0;

        foreach (var c in chances)
        {
            current += c.chance;
            if (roll <= current) return c.tier;
        }
        
        return chances.Last().tier;
    }
}
