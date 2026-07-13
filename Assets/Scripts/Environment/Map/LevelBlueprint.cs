using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomEventType
{
    None,
    Elite,
    Blacksmith,
    CursedChest,
    Statue,
    Reward,
    Story,
    HighMagicFactor,
    Rune,
    Teleport,
    EchoRoom
}

[Serializable]
public class NodeBlueprint
{
    public string nodeName;
    public RoomNodeType roomType;

    [Range(0f, 1f)]
    public float spawnChance = 1f;
    
    public bool forceDirection;
    public ExitDirection requiredDir;
    public List<int> childrenIndices = new List<int>();

    [Header("Event Config")]
    public RoomEventType eventType = RoomEventType.None;
    [Range(0f, 1f)] public float eventChance = 0f;

    [HideInInspector] public Vector2 position;
}

[System.Serializable]
public struct EchoNodeRate
{
    public GameObject nodePrefab;
    [Range(0, 100)] public float weight;
}

[System.Serializable]
public class EventTypeLimit
{
    public RoomEventType eventType;
    public int minRequired = 0;
    public int maxAllowed = 1;
    [HideInInspector] public int currentCount = 0;
    [HideInInspector] public int remainingCandidates = 0;
}

[CreateAssetMenu(fileName = "New Level Blueprint", menuName = "Echoes/Level Blueprint")]
public class LevelBlueprint : ScriptableObject
{
    [Header("Graph Nodes")]
    public List<NodeBlueprint> nodes = new List<NodeBlueprint>();

    [Header("Room Prefab Pools")]
    public List<GameObject> startRoomPrefabs = new List<GameObject>();
    public List<GameObject> normalRoomPrefabs = new List<GameObject>();
    public List<GameObject> statueRoomPrefabs = new List<GameObject>();
    public List<GameObject> buffRoomPrefabs = new List<GameObject>();
    public List<GameObject> rewardRoomPrefabs = new List<GameObject>();
    public List<GameObject> eliteRoomPrefabs = new List<GameObject>();
    public List<GameObject> goalRoomPrefabs = new List<GameObject>();
    public List<GameObject> deadEndRoomPrefabs = new List<GameObject>();
    public List<GameObject> storyRoomPrefabs = new List<GameObject>();
    public List<GameObject> echoRoomPrefabs = new List<GameObject>();

    [Header("Generation Limits")]
    public List<EventTypeLimit> eventTypeLimits = new List<EventTypeLimit>();

    [Header("Extraction Node Settings")]
    [Tooltip("Independent chance (0.0 to 1.0) for each anchor to successfully spawn a node.")]
    [Range(0f, 1f)] public float anchorSpawnChance = 0.3f;
    public List<EchoNodeRate> echoNodePool = new List<EchoNodeRate>();
}