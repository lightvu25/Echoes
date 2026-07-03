using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum LootItemType { Equipment, Relic, Consumable, Currency }

public struct DropResult
{
    public GameObject prefab;
    public int totalAmount;
    public LootItemType type;
}

[Serializable]
public class LootItem
{
    [Tooltip("Prefab instantiated when this item drops.")]
    public GameObject itemPrefab;

    [Tooltip("Independent drop probability, 0–100.")]
    [Range(0f, 100f)] public float dropChance = 25f;

    [Tooltip("Tier 1 = common, 2 = rare, 3 = epic.")]
    [Range(1, 3)] public int itemTier = 1;

    public int minAmount = 1;
    public int maxAmount = 1;

    public LootItemType type;
}

[CreateAssetMenu(fileName = "LootTable", menuName = "Echoes/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootItem> lootItems = new();
    public List<DropResult> GetDrops(float chanceMultiplier, int minTierAllowed)
    {
        var drops = new List<DropResult>();

        foreach (var item in lootItems)
        {
            if (item.itemPrefab == null) continue;

            if (item.type != LootItemType.Consumable && item.type != LootItemType.Currency && item.itemTier < minTierAllowed) continue;

            float finalChance = item.dropChance * chanceMultiplier;
            if (Random.Range(0f, 100f) <= finalChance)
            {
                drops.Add(new DropResult
                {
                    prefab = item.itemPrefab,
                    totalAmount = Random.Range(item.minAmount, item.maxAmount + 1),
                    type = item.type
                });
            }
        }

        return drops;
    }
}
