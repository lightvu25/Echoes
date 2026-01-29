using System.Collections.Generic;
using System;

public class ProfileData
{
    public int totalGold;
    public List<string> unlockedWeaponIDs = new List<string>();
    public int deaths;
    public int kills;
    public int timeRun;

    public ProfileData()
    {
        totalGold = 0;
        unlockedWeaponIDs.Add("Sword_Basic");
    }
}