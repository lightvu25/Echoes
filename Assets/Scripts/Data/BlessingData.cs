using UnityEngine;
public enum BlessingPath 
{ 
    Vitality, 
    Sorcery, 
    Resonance 
}
[CreateAssetMenu(fileName = "NewBlessing", menuName = "Data/Blessing", order = 20)]
public class BlessingData : ScriptableObject
{
    public string buffName;
    [TextArea(2, 4)]
    public string description;
    
    [Header("Visuals")]
    public Sprite icon;

    public BlessingPath path;
    public string specialEffectID;
    
    public int flatBonusHP;
    public int flatBonusSkill;
    public int flatBonusResonance;
    public int grantedVitality;
    public int grantedSorcery;
    public int grantedResonance;
}