using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MindNodeModifierData", menuName = "Echoes/Mind Scene/Node Modifier Data")]
public class MindNodeModifierData : ScriptableObject
{
    [Header("Rewards")]
    [Tooltip("Percentage bonus to finding Relics in the next level (e.g., 0.05 = 5%).")]
    public float bonusRelicChance = 0f;
    
    [Tooltip("Percentage bonus to finding Equipment in the next level.")]
    public float bonusEquipmentChance = 0f;

    [Tooltip("Percentage bonus to finding Echoes in the next level.")]
    public float bonusEchoChance = 0f;


    [Header("Featured Items")]
    [Tooltip("When enabled, this node boosts a small set of named items instead of the whole reward category.")]
    public bool useFeaturedItemBoost = false;

    [Tooltip("Catalog used to roll the named items shown by this node.")]
    [SerializeField] private RunItemCatalog featuredItemCatalog;

    [Min(1)] public int minFeaturedItems = 2;
    [Min(1)] public int maxFeaturedItems = 3;

    [Min(1f)]
    [Tooltip("Selection weight applied to each featured item. 3 means it is three times as likely as a normal item in the same pool.")]
    public float featuredItemWeightMultiplier = 3f;

    public RunItemCatalog FeaturedItemCatalog => featuredItemCatalog;

    [Header("Risks")]
    [Tooltip("Flat amount of Magic Toxicity added immediately upon accepting this node.")]
    public int magicToxicityIncrease = 0;

    [Tooltip("Multiplier for how many enemies spawn in the next level (1.0 = normal).")]
    public float enemyDensityMultiplier = 1.0f;

    [Tooltip("List of special Elite enemies injected into the spawn pool for the next level.")]
    public List<string> addedEliteEnemyTypes = new List<string>();

    public List<ItemBaseData> RollFeaturedItems(ItemCategory category)
    {
        var candidates = new List<ItemBaseData>();
        if (!useFeaturedItemBoost || featuredItemCatalog == null) return candidates;

        foreach (ItemBaseData item in featuredItemCatalog.Items)
        {
            if (item == null || item.Category != category || string.IsNullOrWhiteSpace(item.itemID)) continue;
            candidates.Add(item);
        }

        int minimum = Mathf.Max(1, minFeaturedItems);
        int maximum = Mathf.Max(minimum, maxFeaturedItems);
        int count = Mathf.Min(candidates.Count, Random.Range(minimum, maximum + 1));

        for (int i = 0; i < count; i++)
        {
            int swapIndex = Random.Range(i, candidates.Count);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        if (candidates.Count > count)
            candidates.RemoveRange(count, candidates.Count - count);

        return candidates;
    }
}
