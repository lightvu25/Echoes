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
    public string RelicID;
    public string RelicNameEN;
    public RelicRarity Rarity;
    public string FactionBonus;
    [TextArea(3, 5)] public string CoreEffect;
    public Sprite RelicIcon;
}
