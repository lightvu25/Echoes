using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MindNodeModifierData", menuName = "Echoes/Mind Scene/Node Modifier Data")]
public class MindNodeModifierData : ScriptableObject
{
    [Header("Rewards")]
    [Tooltip("Percentage bonus to finding Relics in the next level (e.g., 0.05 = 5%).")]
    public float bonusRelicChance = 0f;
    
    [Tooltip("Percentage bonus to finding Equipment in the next level.")]
    public float bonusEquipmentChance = 0f;

    [Tooltip("Percentage bonus to finding Echoes in the next level.")]
    public float bonusEchoChance = 0f;



    [Header("Risks")]
    [Tooltip("Flat amount of Magic Toxicity added immediately upon accepting this node.")]
    public int magicToxicityIncrease = 0;

    [Tooltip("Multiplier for how many enemies spawn in the next level (1.0 = normal).")]
    public float enemyDensityMultiplier = 1.0f;

    [Tooltip("List of special Elite enemies injected into the spawn pool for the next level.")]
    public List<string> addedEliteEnemyTypes = new List<string>();
}
