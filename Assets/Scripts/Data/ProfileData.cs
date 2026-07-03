using System.Collections.Generic;
using System;

public class ProfileData
{
    public int totalGold;
    public int bankedAstralShards;
    public int deaths;
    public int kills;
    public int timeRun;
    public int bonusStartingMaxHP;
    public List<string> unlockedWeaponIDs = new List<string>();
    public List<string> unlockedSkillIDs = new List<string>();
    public List<string> persistenceKeys = new List<string>();

    public List<int> attemptLevelKeys   = new List<int>();
    public List<int> attemptLevelValues = new List<int>();

    public int GetLevelAttempts(int levelIndex)
    {
        int idx = attemptLevelKeys.IndexOf(levelIndex);
        return idx >= 0 ? attemptLevelValues[idx] : 0;
    }

    public void IncrementLevelAttempt(int levelIndex)
    {
        int idx = attemptLevelKeys.IndexOf(levelIndex);
        if (idx >= 0)
            attemptLevelValues[idx]++;
        else
        {
            attemptLevelKeys.Add(levelIndex);
            attemptLevelValues.Add(1);
        }
    }

    public ProfileData()
    {
        totalGold        = 0;
        bankedAstralShards = 0;
        bonusStartingMaxHP = 0;
        unlockedWeaponIDs.Add("Sword_Basic");
    }

    public bool HasSkill(string skillID) =>
        unlockedSkillIDs != null && unlockedSkillIDs.Contains(skillID);
}