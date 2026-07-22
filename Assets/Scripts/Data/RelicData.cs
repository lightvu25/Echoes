using UnityEngine;

public enum RelicRarity
{
    Common,
    Rare,
    Legendary
}

[CreateAssetMenu(fileName = "New Relic", menuName = "Data/Relic")]
public class RelicData : ItemBaseData
{
    public override ItemCategory Category => ItemCategory.Relic;

    [Header("Relic Info")]
    public RelicRarity Rarity;
    public string FactionBonus;

    // --- Runtime Data ---
    [HideInInspector] public int bonusVitality = 0;
    [HideInInspector] public int bonusSorcery = 0;
    [HideInInspector] public int bonusResonance = 0;
    [HideInInspector] public bool isRuntimeInitialized = false;

    public void InitRuntime()
    {
        if (isRuntimeInitialized) return;

        bonusVitality = 0;
        bonusSorcery = 0;
        bonusResonance = 0;

        int numStats = 0;
        if (Rarity == RelicRarity.Common) numStats = 1;
        else if (Rarity == RelicRarity.Rare) numStats = 2;
        else if (Rarity == RelicRarity.Legendary) numStats = 3;

        for (int i = 0; i < numStats; i++)
        {
            int roll = UnityEngine.Random.Range(0, 3);
            if (roll == 0) bonusVitality++;
            else if (roll == 1) bonusSorcery++;
            else if (roll == 2) bonusResonance++;
        }

        // Update FactionBonus string to reflect actual stats
        string statsText = "";
        if (bonusVitality > 0) statsText += $"+{bonusVitality} Vitality (+{bonusVitality * 10} HP)\n";
        if (bonusSorcery > 0) statsText += $"+{bonusSorcery} Sorcery\n";
        if (bonusResonance > 0) statsText += $"+{bonusResonance} Resonance\n";
        if (!string.IsNullOrEmpty(statsText)) FactionBonus = statsText.Trim();

        isRuntimeInitialized = true;
    }
}
