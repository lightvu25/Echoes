using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>Identifies the category of a loot item for tier-restriction logic.</summary>
public enum LootItemType { Equipment, Relic, Consumable, Currency }

/// <summary>The rolled result from a LootTable.</summary>
public struct DropResult
{
    public GameObject prefab;
    public int totalAmount;
    public LootItemType type;
}

/// <summary>A single entry in a LootTable.</summary>
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

/// <summary>
/// ScriptableObject that defines the loot pool for a chest or enemy.
/// Each item rolls its own chance independently.
/// </summary>
[CreateAssetMenu(fileName = "LootTable", menuName = "Echoes/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootItem> lootItems = new();

    /// <summary>
    /// Evaluates each entry and returns the prefabs that passed their roll.
    /// </summary>
    /// <param name="chanceMultiplier">Scales every drop chance (1f = normal, 1.5f = elite).</param>
    /// <param name="minTierAllowed">Non-consumable items below this tier are skipped.</param>
    public List<DropResult> GetDrops(float chanceMultiplier, int minTierAllowed)
    {
        var drops = new List<DropResult>();

        foreach (var item in lootItems)
        {
            if (item.itemPrefab == null) continue;

            // Tier gate: only consumables and currency bypass the minimum tier restriction.
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
