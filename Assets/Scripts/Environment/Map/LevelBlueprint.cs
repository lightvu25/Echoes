using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomEventType
{
    None = 0,
    Elite = 1,
    Shop = 2,
    CursedChest = 3,
    Reward = 5,
    Story = 6,
    HighMagicFactor = 7,
    EchoRoom = 10
}

public enum DynamicInjectionType
{
    None,
    Teleport,
    Altar,
    Shrine,
    Echo
}

[Serializable]
public struct DynamicInjectionConfig
{
    public DynamicInjectionType injectionType;
    [Range(0f, 1f)] public float spawnChance;
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

    [Header("Event Config (Primary)")]
    public RoomEventType eventType = RoomEventType.None;
    [Range(0f, 1f)] public float eventChance = 0f;

    [Header("Dynamic Injections")]
    public List<DynamicInjectionConfig> dynamicInjections = new List<DynamicInjectionConfig>();

    [HideInInspector] public Vector2 position;
}

[System.Serializable]
public struct EchoNodeRate
{
    public GameObject nodePrefab;
    [Range(0, 100)] public float weight;
}

[System.Serializable]
public struct EnemyNodeRate
{
    public GameObject enemyPrefab;
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

[System.Serializable]
public class InjectionTypeLimit
{
    public DynamicInjectionType injectionType;
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
    public List<GameObject> goalRoomPrefabs = new List<GameObject>();
    public List<GameObject> deadEndRoomPrefabs = new List<GameObject>();
    public List<GameObject> rewardRoomPrefabs = new List<GameObject>();
    public List<GameObject> eliteRoomPrefabs = new List<GameObject>();
    public List<GameObject> storyRoomPrefabs = new List<GameObject>();
    public List<GameObject> echoRoomPrefabs = new List<GameObject>();
    public List<GameObject> cursedChestRoomPrefabs = new List<GameObject>();
    public List<GameObject> highMagicFactorRoomPrefabs = new List<GameObject>();
    public List<GameObject> shopRoomPrefabs = new List<GameObject>();
    
    [Header("Generation Limits")]
    public List<EventTypeLimit> eventTypeLimits = new List<EventTypeLimit>();
    public List<InjectionTypeLimit> injectionLimits = new List<InjectionTypeLimit>();

    [Header("Extraction Node Settings")]
    [Range(0f, 1f)] public float anchorSpawnChance = 0.3f;
    public List<EchoNodeRate> echoNodePool = new List<EchoNodeRate>();

    [Header("Enemy Node Settings")]
    [Range(0f, 1f)] public float groundEnemySpawnChance = 0.6f;
    public List<EnemyNodeRate> groundEnemyPool = new List<EnemyNodeRate>();
    
    [Range(0f, 1f)] public float airEnemySpawnChance = 0.4f;
    public List<EnemyNodeRate> airEnemyPool = new List<EnemyNodeRate>();

    [Header("Elite Enemy Settings")]
    [Tooltip("Drag your Elite prefabs here from Assets/Prefab/Enemy/Elites so they can be spawned by Toxicity.")]
    public List<GameObject> availableElitePrefabs = new List<GameObject>();
}