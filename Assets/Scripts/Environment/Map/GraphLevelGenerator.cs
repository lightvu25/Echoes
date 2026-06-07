using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum RoomNodeType
{
    Start,
    Normal,
    Statue,
    Buff,
    Reward,
    Elite,
    Goal,
    DeadEnd
}

public class GraphLevelGenerator : BaseLevelGenerator
{
    // ------------------------------------------------------------------ //
    //  Designer-Authored Sequence                                         //
    // ------------------------------------------------------------------ //

    [Header("Main Path Sequence")]
    [Tooltip("The ordered list of room types that defines the dungeon flow. " +
             "The first entry MUST be Start, the last should be Goal.")]
    [SerializeField] private List<RoomNodeType> mainPathSequence = new List<RoomNodeType>
    {
        RoomNodeType.Start,
        RoomNodeType.Normal,
        RoomNodeType.Normal,
        RoomNodeType.Statue,
        RoomNodeType.Normal,
        RoomNodeType.Reward,
        RoomNodeType.Goal
    };

    // Room Prefab Pools (one per node type)

    [Header("Room Prefab Pools")]
    [Tooltip("Prefabs for the starting room. Typically only one.")]
    [SerializeField] private List<GameObject> startRoomPrefabs = new List<GameObject>();

    [Tooltip("Generic combat / traversal rooms.")]
    [SerializeField] private List<GameObject> normalRoomPrefabs = new List<GameObject>();

    [Tooltip("Rooms containing a checkpoint statue.")]
    [SerializeField] private List<GameObject> statueRoomPrefabs = new List<GameObject>();

    [Tooltip("Rooms that grant a temporary buff.")]
    [SerializeField] private List<GameObject> buffRoomPrefabs = new List<GameObject>();

    [Tooltip("Rooms with treasure / reward chests.")]
    [SerializeField] private List<GameObject> rewardRoomPrefabs = new List<GameObject>();

    [Tooltip("Rooms containing an elite / mini-boss encounter.")]
    [SerializeField] private List<GameObject> eliteRoomPrefabs = new List<GameObject>();

    [Tooltip("The final room of the level (boss gate, exit portal, etc.).")]
    [SerializeField] private List<GameObject> goalRoomPrefabs = new List<GameObject>();

    [Tooltip("Optional dead-end / side rooms for branching paths.")]
    [SerializeField] private List<GameObject> deadEndRoomPrefabs = new List<GameObject>();

    // ------------------------------------------------------------------ //
    //  Overlap Prevention                                                  //
    // ------------------------------------------------------------------ //

    [Header("Overlap Prevention")]
    [SerializeField] private float minimumRoomSpacing = 2f;
    [SerializeField] private float shrinkFactor = 0.70f;

    // ------------------------------------------------------------------ //
    //  Runtime State                                                       //
    // ------------------------------------------------------------------ //

    private readonly List<Bounds> _placedBounds = new List<Bounds>();
    private readonly List<Room> _placedRoomComponents = new List<Room>();

    private Transform _playerSpawnPoint;

    public override void GenerateMap(int levelNumber)
    {
        // Clear previous generation.
        ClearGraphState();

        if (mainPathSequence == null || mainPathSequence.Count == 0)
        {
            Debug.LogError("[GraphLevelGenerator] mainPathSequence is empty. " +
                           "Cannot generate a level without at least a Start node.");
            return;
        }

        // -------------------------------------------------------------- //
        //  Step 1: Spawn the Start room at the origin.                    //
        // -------------------------------------------------------------- //
        Room previousRoom = SpawnStartRoom();
        if (previousRoom == null)
        {
            Debug.LogError("[GraphLevelGenerator] Failed to spawn the Start room. Aborting.");
            return;
        }

        // -------------------------------------------------------------- //
        //  Step 2: Walk the sequence, snapping each room to the previous. //
        // -------------------------------------------------------------- //
        for (int i = 1; i < mainPathSequence.Count; i++)
        {
            RoomNodeType nodeType = mainPathSequence[i];
            List<GameObject> pool = GetPoolForType(nodeType);

            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning($"[GraphLevelGenerator] No prefabs in pool for " +
                                 $"node type '{nodeType}' at sequence index {i}. Skipping.");
                continue;
            }

            Room nextRoom = TrySnapRoom(previousRoom, pool);

            if (nextRoom == null)
            {
                Debug.LogWarning($"[GraphLevelGenerator] Could not place room of type " +
                                 $"'{nodeType}' at sequence index {i}. " +
                                 $"All exits exhausted or all prefabs overlap. Skipping.");
                continue;
            }

            previousRoom = nextRoom;
        }
        RuntimeTilemapMerger.Instance.MergeAllRooms(_spawnedRooms);

        Debug.Log($"[GraphLevelGenerator] Generation complete. " +
                  $"Placed {_spawnedRooms.Count}/{mainPathSequence.Count} rooms.");

        NotifyGenerationComplete();
    }

    public override Transform GetPlayerSpawnPoint()
    {
        if (_playerSpawnPoint != null)
            return _playerSpawnPoint;

        // Fallback: create one at origin.
        Debug.LogWarning("[GraphLevelGenerator] No PlayerSpawnPoint found. Using origin.");
        GameObject fallback = new GameObject("FallbackPlayerSpawn");
        fallback.transform.position = Vector3.zero;
        return fallback.transform;
    }

    // ================================================================== //
    //  START ROOM                                                         //
    // ================================================================== //

    private Room SpawnStartRoom()
    {
        List<GameObject> pool = GetPoolForType(RoomNodeType.Start);
        if (pool == null || pool.Count == 0)
        {
            Debug.LogError("[GraphLevelGenerator] Start room pool is empty!");
            return null;
        }

        GameObject prefab = pool[Random.Range(0, pool.Count)];
        GameObject startObj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        startObj.name = "Room_Start_0";

        Room room = startObj.GetComponent<Room>();
        if (room == null)
        {
            Debug.LogError("[GraphLevelGenerator] Start room prefab is missing a Room component!");
            Destroy(startObj);
            return null;
        }

        // Cache the player spawn point.
        Transform spawnPt = startObj.transform.Find("PlayerSpawnPoint");
        _playerSpawnPoint = spawnPt != null ? spawnPt : startObj.transform;

        // Register the room.
        RegisterRoom(startObj, room);

        return room;
    }

    // ================================================================== //
    //  EXIT-TO-ENTRANCE SNAPPING                                          //
    // ================================================================== //

    private Room TrySnapRoom(Room previousRoom, List<GameObject> pool)
    {
        List<RoomExit> availableExits = new List<RoomExit>(previousRoom.Exits);
        ShuffleList(availableExits);

        List<int> prefabIndices = new List<int>();
        for (int i = 0; i < pool.Count; i++) prefabIndices.Add(i);

        foreach (RoomExit exitA in availableExits)
        {
            ExitDirection requiredDir = Room.GetOpposite(exitA.direction);

            ShuffleList(prefabIndices);

            foreach (int idx in prefabIndices)
            {
                GameObject prefab = pool[idx];
                if (prefab == null) continue;

                GameObject candidate = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                Room candidateRoom = candidate.GetComponent<Room>();

                if (candidateRoom == null)
                {
                    Debug.LogWarning($"[GraphLevelGenerator] Prefab '{prefab.name}' " +
                                     "is missing a Room component. Skipping.");
                    Destroy(candidate);
                    continue;
                }

                if (!candidateRoom.TryGetExitInDirection(requiredDir, out RoomExit entranceB))
                {
                    Destroy(candidate);
                    continue;
                }

                if (exitA.exitPoint == null || entranceB.exitPoint == null)
                {
                    Debug.LogWarning($"[GraphLevelGenerator] Exit point Transform is null " +
                                     $"on '{previousRoom.name}' or '{candidate.name}'. Skipping.");
                    Destroy(candidate);
                    continue;
                }

                Vector3 offset = exitA.exitPoint.position - entranceB.exitPoint.position;
                candidate.transform.position += offset;

                Bounds candidateBounds = GetRoomBounds(candidateRoom);

                if (DoesOverlapExistingRooms(candidateBounds))
                {
                    Destroy(candidate);
                    continue;
                }

                candidate.name = $"Room_{_spawnedRooms.Count}";
                previousRoom.RemoveExit(exitA);
                candidateRoom.RemoveExit(entranceB);

                RegisterRoom(candidate, candidateRoom);

                EnemySpawner[] spawners = candidate.GetComponentsInChildren<EnemySpawner>();
                foreach (EnemySpawner spawner in spawners)
                {
                    spawner.Init();
                }

                return candidateRoom;
            }
        }

        return null;
    }

    // ================================================================== //
    //  OVERLAP DETECTION                                                  //
    // ================================================================== //

    private bool DoesOverlapExistingRooms(Bounds newBounds)
    {
        Bounds shrunk = new Bounds(newBounds.center, newBounds.size * shrinkFactor);

        foreach (Bounds existing in _placedBounds)
            if (shrunk.Intersects(existing)) return true;

        foreach (Room placed in _placedRoomComponents)
        {
            if (placed == null) continue;
            if (Vector3.Distance(newBounds.center, placed.transform.position) < minimumRoomSpacing)
                return true;
        }

        return false;
    }

    // ================================================================== //
    //  ROOM BOUNDS HELPER                                                 //
    // ================================================================== //
    private Bounds GetRoomBounds(Room room)
    {
        Collider2D col = room.GetComponent<Collider2D>();
        if (col != null)
            return col.bounds;

        Renderer[] renderers = room.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                combined.Encapsulate(renderers[i].bounds);
            return combined;
        }

        return new Bounds(room.transform.position, new Vector3(20f, 12f, 1f));
    }

    // ================================================================== //
    //  POOL LOOKUP                                                        //
    // ================================================================== //
    private List<GameObject> GetPoolForType(RoomNodeType nodeType)
    {
        return nodeType switch
        {
            RoomNodeType.Start   => startRoomPrefabs,
            RoomNodeType.Normal  => normalRoomPrefabs,
            RoomNodeType.Statue  => statueRoomPrefabs,
            RoomNodeType.Buff    => buffRoomPrefabs,
            RoomNodeType.Reward  => rewardRoomPrefabs,
            RoomNodeType.Elite   => eliteRoomPrefabs,
            RoomNodeType.Goal    => goalRoomPrefabs,
            RoomNodeType.DeadEnd => deadEndRoomPrefabs,
            _ => null
        };
    }

    // ================================================================== //
    //  HELPER UTILITIES                                                   //
    // ================================================================== //
    private void RegisterRoom(GameObject roomObj, Room roomComponent)
    {
        _spawnedRooms.Add(roomObj);
        _placedRoomComponents.Add(roomComponent);
        _placedBounds.Add(GetRoomBounds(roomComponent));
    }

    private void ClearGraphState()
    {
        ClearSpawnedRooms();
        _placedBounds.Clear();
        _placedRoomComponents.Clear();
        _playerSpawnPoint = null;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ================================================================== //
    //  EDITOR GIZMOS                                                      //
    // ================================================================== //

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_placedBounds == null || _placedBounds.Count == 0) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        foreach (Bounds b in _placedBounds)
            Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}
