using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject representing a node in the Memory Web.
/// Each node defines map modifiers and unlocked abilities that shape
/// the next procedurally generated level when chosen at a Hub exit altar.
/// </summary>
[CreateAssetMenu(fileName = "NewMemoryNode", menuName = "Echoes/Memory Node Data")]
public class MemoryNodeData : ScriptableObject
{
    [Header("Identity")]
    public string nodeID;
    public string nodeName;
    [TextArea] public string description;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Gameplay Effects")]
    public List<MapModifier> mapModifiers;
    public List<string> unlockedAbilities;

    [Header("Graph")]
    public bool isUnlocked;
    public List<string> connectedNodeIDs;
}
