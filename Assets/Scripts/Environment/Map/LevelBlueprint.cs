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
    Rune
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

[CreateAssetMenu(fileName = "New Level Blueprint", menuName = "Echoes/Level Blueprint")]
public class LevelBlueprint : ScriptableObject
{
    public List<NodeBlueprint> nodes = new List<NodeBlueprint>();
}