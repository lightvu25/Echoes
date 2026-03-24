using UnityEngine;

[CreateAssetMenu(fileName = "New Buff Data", menuName = "Data/Buff Data")]
public class BuffData : ScriptableObject
{
    public string buffID;
    public string buffName;
    [TextArea(3, 5)] public string description;
    public Sprite icon;
    public int banishCost = 10;
}
