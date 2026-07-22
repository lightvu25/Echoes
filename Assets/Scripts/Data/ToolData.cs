using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Data/Tool")]
public class ToolData : ItemBaseData
{
    public override ItemCategory Category => ItemCategory.Tool;

    [Header("Tool Properties")]
    [Tooltip("Number of times this tool can be used before breaking. 0 means infinite.")]
    public int durability;
    
    [Tooltip("Maximum number of uses or charges.")]
    public int maxUses;

    [Tooltip("Cooldown in seconds before this tool can be used again.")]
    public float cooldown;
}
