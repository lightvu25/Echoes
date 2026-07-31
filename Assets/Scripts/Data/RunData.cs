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
    public int currentLevel = 1;
    public int availableCuts = 1;
    public int currentExp;
    public int currentAstralShards;
    public List<string> currentRelics = new List<string>();
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

    [Header("Difficulty Multipliers")]
    public float enemyDensityMultiplier = 1.0f;
    public List<string> addedEliteEnemyTypes = new List<string>();
    public int bonusSorcery = 0;
    public int bonusResonance = 0;
    public int bonusVitality = 0;
    public int vitalityShrinesTaken = 0;
    public int sorceryShrinesTaken = 0;
    public int resonanceShrinesTaken = 0;
    public int unlockedEchoSlots = 1; // Legacy, keep for migration
    public int unlockedRelicSlots = 1; // Legacy
    public int unlockedEquipmentSlots = 1; // Legacy
    public int availableUnlockPoints = 0;
    public List<int> unlockedEchoIndices = new List<int> { 0 };
    public List<int> unlockedRelicIndices = new List<int> { 0 };
    public List<int> unlockedToolIndices = new List<int> { 0 };
    public const int MAX_SLOTS = 10;
    public List<string> exploredRooms = new List<string>();
    public List<string> activeBlessingEffects = new List<string>();
    public List<string> currentLevelBurdens = new List<string>();
}
