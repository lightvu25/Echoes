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

    public List<string> partialSkillIDs = new List<string>();
    public List<int> partialSkillAmounts = new List<int>();

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

    public int GetInvestedAmount(string skillID)
    {
        if (partialSkillIDs == null || partialSkillAmounts == null) return 0;
        int idx = partialSkillIDs.IndexOf(skillID);
        return idx >= 0 ? partialSkillAmounts[idx] : 0;
    }

    public void SetInvestedAmount(string skillID, int amount)
    {
        if (partialSkillIDs == null) partialSkillIDs = new List<string>();
        if (partialSkillAmounts == null) partialSkillAmounts = new List<int>();

        int idx = partialSkillIDs.IndexOf(skillID);
        if (idx >= 0)
        {
            if (amount <= 0)
            {
                partialSkillIDs.RemoveAt(idx);
                partialSkillAmounts.RemoveAt(idx);
            }
            else
            {
                partialSkillAmounts[idx] = amount;
            }
        }
        else if (amount > 0)
        {
            partialSkillIDs.Add(skillID);
            partialSkillAmounts.Add(amount);
        }
    }
}