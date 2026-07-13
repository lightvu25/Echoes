using UnityEngine;

public enum NodeType
{
    Start,
    Reward,
    MindGarden,
    Echo,
    Relic,
    MapExit,
    ChallengeNoHit,
    ChallengeSpeedrun
}

[System.Serializable]
public class NodeConnection
{
    [Tooltip("The target MindNode this branch connects to.")]
    public MindNode targetNode;

    [Tooltip("Is this branch physically cut by the player?")]
    public bool isCut;

    [Tooltip("Is this branch manually connected by the player?")]
    public bool isConnected;

    [Tooltip("Magic toxicity applied if this branch is cut.")]
    public int magicToxicityCost = 1;

    [Tooltip("Relic spawn percentage bonus applied if this branch is connected.")]
    public float relicBonusPercentage = 0.05f;
}

/// <summary>
/// Represents a node in the MindScene graph.
/// Links to adjacent nodes via NodeConnections to handle branching mechanics.
/// </summary>
public class MindNode : MonoBehaviour
{
    [Header("Node Settings")]
    public NodeType nodeType = NodeType.Reward;

    [Header("Challenge Requirements")]
    [Tooltip("Enemies killed without taking damage (for No-hit doors)")]
    public int requiredNoHitKills = 30;
    
    [Tooltip("Time limit in seconds to reach the goal (for Speedrun doors)")]
    public float requiredSpeedrunTime = 120f;

    [Header("Graph Connections")]
    public NodeConnection upConnection;
    public NodeConnection downConnection;
    public NodeConnection leftConnection;
    public NodeConnection rightConnection;
}
