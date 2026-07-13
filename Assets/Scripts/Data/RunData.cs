using System.Collections.Generic;
using System;

public class RunData
{
    public int currentHealth;
    public int maxHealth;
    public string currentSceneName;
    public int runGold;
    public int currentLevel = 1;
    public int currentExp;
    public int currentAstralShards;
    public List<string> currentRelics = new List<string>();
    public int mapSeed;
    public int levelNumber = 1;
    public int bonusSorcery = 0;
    public int bonusResonance = 0;
    public int bonusVitality = 0;
    public int vitalityShrinesTaken = 0;
    public int sorceryShrinesTaken = 0;
    public int resonanceShrinesTaken = 0;
    public int unlockedEchoSlots = 1;
    public int unlockedRelicSlots = 1;
    public int unlockedItemSlots = 1;
    public const int MAX_SLOTS = 4;
    public List<string> exploredRooms = new List<string>();
    public List<string> activeBlessingEffects = new List<string>();
    public List<string> currentLevelBurdens = new List<string>();
}
