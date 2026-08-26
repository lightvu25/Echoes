using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public class RunData
{
    public int currentHealth;
    public int maxHealth;
    public string currentSceneName;
    public string currentLevelName = "The Abyss";
    public int runGold;
    public int peakRunGold = 0;
    public int currentLevel = 1;
    public int availableCuts = 1;
    public int currentExp;
    public int currentAstralShards;
    public List<string> currentRelics = new List<string>();
    public List<string> equippedEchoIds = new List<string>();
    public List<string> equippedRelicIds = new List<string>();
    public List<string> equippedToolIds = new List<string>();
    public int activeEchoIndex = 0;
    public int mapSeed;
    public int levelNumber = 1;
    public float currentLevelTime = 0f;
    public int currentLevelNoHitKills = 0;
    public int magicToxicity = 0;
    public float relicBonusModifier = 1.0f;
    public float bonusEquipmentChance = 0f;
    public float bonusEchoChance = 0f;
    public float bonusRelicChance = 0f;
    public float currentLevelRelicMultiplier = 1f;
    public float currentLevelEchoMultiplier = 1f;
    public float currentLevelEquipmentMultiplier = 1f;

    [Header("Guaranteed Level Drops")]
    public int minGuaranteedRelics = 0;
    public int minGuaranteedEchoes = 0;
    public int minGuaranteedEquipment = 0;

    [Header("Featured Level Loot")]
    public List<string> featuredRelicIds = new List<string>();
    public List<string> featuredEchoIds = new List<string>();
    public List<string> featuredEquipmentIds = new List<string>();
    public float featuredItemWeightMultiplier = 1f;

    [Header("Difficulty Multipliers")]
    public float enemyDensityMultiplier = 1.0f;
    public List<string> addedEliteEnemyTypes = new List<string>();
    public int bonusSorcery = 0;
    public int bonusResonance = 0;
    public int bonusVitality = 0;
    public int vitalityShrinesTaken = 0;
    public int sorceryShrinesTaken = 0;
    public int resonanceShrinesTaken = 0;
    public int unlockedEchoSlots = 1;
    public int unlockedRelicSlots = 1;
    public int unlockedEquipmentSlots = 1;
    public int availableUnlockPoints = 0;
    public List<int> unlockedEchoIndices = new List<int> { 0 };
    public List<int> unlockedRelicIndices = new List<int> { 0 };
    public List<int> unlockedToolIndices = new List<int> { 0 };
    public const int MAX_SLOTS = 10;
    public List<string> exploredRooms = new List<string>();
    public List<string> activeBlessingEffects = new List<string>();
    public List<string> currentLevelBurdens = new List<string>();

    public void ClearFeaturedLoot()
    {
        featuredRelicIds = new List<string>();
        featuredEchoIds = new List<string>();
        featuredEquipmentIds = new List<string>();
        featuredItemWeightMultiplier = 1f;
    }

    public void SetFeaturedLoot(ItemCategory category, IReadOnlyList<ItemBaseData> items, float weightMultiplier)
    {
        List<string> target = GetOrCreateFeaturedIds(category);
        target.Clear();

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                string itemId = items[i]?.itemID;
                if (!string.IsNullOrWhiteSpace(itemId) && !target.Contains(itemId))
                    target.Add(itemId);
            }
        }

        featuredItemWeightMultiplier = Mathf.Max(1f, weightMultiplier);
    }

    public bool IsFeaturedLoot(ItemCategory category, string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && GetOrCreateFeaturedIds(category).Contains(itemId);
    }

    public float GetFeaturedLootWeight(ItemCategory category, string itemId)
    {
        return IsFeaturedLoot(category, itemId) ? Mathf.Max(1f, featuredItemWeightMultiplier) : 1f;
    }

    private List<string> GetOrCreateFeaturedIds(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Relic:
                return featuredRelicIds ??= new List<string>();
            case ItemCategory.Echo:
                return featuredEchoIds ??= new List<string>();
            default:
                return featuredEquipmentIds ??= new List<string>();
        }
    }
}
