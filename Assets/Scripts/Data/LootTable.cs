using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum LootItemType { Equipment, Relic, Consumable, Currency, Echo }

public struct DropResult
{
    public GameObject prefab;
    public int totalAmount;
    public LootItemType type;
}

public struct LootBonuses
{
    public float relicBonus;
    public float echoBonus;
    public float equipmentBonus;
    
    public float roomRelicMultiplier;
    public float roomEchoMultiplier;
    public float roomEquipmentMultiplier;
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < lootItems.Count; i++)
        {
            LootItem item = lootItems[i];
            if (item?.itemPrefab == null) continue;

            if (item.itemPrefab.TryGetComponent(out ResourceDrop _) &&
                item.type != LootItemType.Currency && item.type != LootItemType.Consumable)
            {
                Debug.LogError($"[LootTable] Entry {i} in '{name}' uses a ResourceDrop prefab as {item.type}. Assign the correct item prefab or category.", this);
            }
        }
    }
#endif

    public List<DropResult> GetDrops(float chanceMultiplier, int minTierAllowed, LootBonuses bonuses = default)
    {
        var drops = new List<DropResult>();

        foreach (var item in lootItems)
        {
            if (item.itemPrefab == null) continue;

            if (item.type != LootItemType.Consumable && item.type != LootItemType.Currency && item.itemTier < minTierAllowed) continue;

            float typedBonus = 0f;
            float roomMultiplier = 1f;

            switch (item.type)
            {
                case LootItemType.Relic:
                    typedBonus = bonuses.relicBonus;
                    roomMultiplier = bonuses.roomRelicMultiplier == 0f ? 1f : bonuses.roomRelicMultiplier;
                    break;
                case LootItemType.Echo:
                    typedBonus = bonuses.echoBonus;
                    roomMultiplier = bonuses.roomEchoMultiplier == 0f ? 1f : bonuses.roomEchoMultiplier;
                    break;
                case LootItemType.Equipment:
                    typedBonus = bonuses.equipmentBonus;
                    roomMultiplier = bonuses.roomEquipmentMultiplier == 0f ? 1f : bonuses.roomEquipmentMultiplier;
                    break;
            }

            float rawChance = (item.dropChance + (typedBonus * 100f)) * chanceMultiplier * roomMultiplier;

            bool isGuaranteed = false;
            var run = GameSession.Instance?.currentRun;
            if (run != null)
            {
                if (item.type == LootItemType.Relic && run.minGuaranteedRelics > 0)
                {
                    isGuaranteed = true;
                    run.minGuaranteedRelics--;
                }
                else if (item.type == LootItemType.Echo && run.minGuaranteedEchoes > 0)
                {
                    isGuaranteed = true;
                    run.minGuaranteedEchoes--;
                }
                else if (item.type == LootItemType.Equipment && run.minGuaranteedEquipment > 0)
                {
                    isGuaranteed = true;
                    run.minGuaranteedEquipment--;
                }
            }

            float finalChance = isGuaranteed ? 100f : Mathf.Min(rawChance, 100f);

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
