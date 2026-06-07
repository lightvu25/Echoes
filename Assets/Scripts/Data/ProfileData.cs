using System.Collections.Generic;
using System;

public class ProfileData
{
    public int totalGold;
    public int memoryFragments;
    public List<string> unlockedWeaponIDs = new List<string>();
    public int deaths;
    public int kills;
    public int timeRun;
    
    public int bankedMems;
    public int bonusStartingMaxHP;

    public ProfileData()
    {
        totalGold = 0;
        memoryFragments = 0;
        bankedMems = 0;
        bonusStartingMaxHP = 0;
        unlockedWeaponIDs.Add("Sword_Basic");
    }
}