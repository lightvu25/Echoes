using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Global Item Pools")]
    [SerializeField] private List<ConsumableData> masterConsumables;
    [SerializeField] private List<RelicData> masterRelics;
    [SerializeField] private List<EchoData> masterEchoes;

    private List<ConsumableData> availableConsumables;
    private List<RelicData> availableRelics;
    private List<EchoData> availableEchoes;

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
        availableConsumables = new List<ConsumableData>(masterConsumables);
        availableRelics = new List<RelicData>(masterRelics);
        availableEchoes = new List<EchoData>(masterEchoes);
    }

    /// <summary>
    /// Pulls a random item from the global pool.
    /// Relics and Echoes are removed from the pool so they cannot be encountered twice in the same run.
    /// </summary>
    public ItemBaseData GetRandomItem(ItemCategory category, int minTier)
    {
        switch (category)
        {
            case ItemCategory.Item: // Consumables
                return GetAndRemoveRandom(availableConsumables.Cast<ItemBaseData>().ToList(), minTier, false);
            case ItemCategory.Relic:
                return GetAndRemoveRandom(availableRelics.Cast<ItemBaseData>().ToList(), minTier, true);
            case ItemCategory.Echo:
                return GetAndRemoveRandom(availableEchoes.Cast<ItemBaseData>().ToList(), minTier, true);
            default:
                return null;
        }
    }

    private ItemBaseData GetAndRemoveRandom(List<ItemBaseData> pool, int minTier, bool removeFromPool)
    {
        var validItems = pool.Where(item => item != null && item.itemTier >= minTier).ToList();
        if (validItems.Count == 0) return null;

        ItemBaseData selected = validItems[Random.Range(0, validItems.Count)];
        
        if (removeFromPool)
        {
            if (selected is RelicData r) availableRelics.Remove(r);
            if (selected is EchoData e) availableEchoes.Remove(e);
        }

        return selected;
    }
}
