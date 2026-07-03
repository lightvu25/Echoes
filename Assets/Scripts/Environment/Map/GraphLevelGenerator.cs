using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;

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

[System.Serializable]
public class RoomTypeLimit
{
    public RoomNodeType roomType;
    public int maxAllowed = 1;
    [HideInInspector] public int currentCount = 0;
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

public class GraphLevelGenerator : BaseLevelGenerator
{
    [Header("Level Blueprint")]
    public LevelBlueprint levelBlueprint;

    private struct NodeTask
    {
        public Room room;
        public int nodeIndex;
        public NodeBlueprint blueprint;
    }

    [Header("Room Prefab Pools")]
    [SerializeField] private List<GameObject> startRoomPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> normalRoomPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> statueRoomPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> buffRoomPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> rewardRoomPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> eliteRoomPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> goalRoomPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> deadEndRoomPrefabs = new List<GameObject>();

    [Header("Generation Limits")]
    public List<RoomTypeLimit> roomTypeLimits = new List<RoomTypeLimit>();
    public List<EventTypeLimit> eventTypeLimits = new List<EventTypeLimit>();

    [Header("Overlap Prevention")]
    [SerializeField] private float minimumRoomSpacing = 2f;
    [SerializeField] private float shrinkFactor = 0.55f;

    // --- Runtime State ---
    private readonly List<Bounds> _placedBounds = new List<Bounds>();
    private readonly List<Room> _placedRoomComponents = new List<Room>();
    private readonly Dictionary<int, List<int>> _activeGraph = new Dictionary<int, List<int>>();
    private readonly Dictionary<GameObject, int> _prefabUsageCount = new Dictionary<GameObject, int>();
    private Transform _playerSpawnPoint;

    public override void GenerateMap(int levelNumber)
    {
        ClearGraphState();
        if (levelBlueprint == null || levelBlueprint.nodes == null || levelBlueprint.nodes.Count == 0) return;

        BuildActiveGraph();

        Room startRoom = SpawnStartRoom();
        if (startRoom == null) return;

        Queue<NodeTask> queue = new Queue<NodeTask>();
        queue.Enqueue(new NodeTask { room = startRoom, nodeIndex = 0, blueprint = levelBlueprint.nodes[0] });

        while (queue.Count > 0)
        {
            NodeTask currentTask = queue.Dequeue();
            Room currentRoom = currentTask.room;
            int currentIdx = currentTask.nodeIndex;

            if (!_activeGraph.TryGetValue(currentIdx, out List<int> activeChildren)) continue;

            activeChildren = activeChildren.OrderByDescending(idx => levelBlueprint.nodes[idx].forceDirection).ToList();
            
            foreach (int childIndex in activeChildren)
            {
                NodeBlueprint childBlueprint = levelBlueprint.nodes[childIndex];
                
                RoomNodeType effectiveRoomType = childBlueprint.roomType;
                RoomTypeLimit rLimit = roomTypeLimits.Find(x => x.roomType == effectiveRoomType);
                if (rLimit != null && rLimit.currentCount >= rLimit.maxAllowed)
                {
                    effectiveRoomType = RoomNodeType.Normal; 
                }

                RoomEventType effectiveEventType = RoomEventType.None;
                
                if (childBlueprint.eventType != RoomEventType.None)
                {
                    EventTypeLimit eLimit = eventTypeLimits.Find(x => x.eventType == childBlueprint.eventType);
                    if (eLimit != null)
                    {
                        bool forceSpawn = false;
                        if (eLimit.currentCount < eLimit.minRequired)
                        {
                            int needed = eLimit.minRequired - eLimit.currentCount;
                            if (eLimit.remainingCandidates <= needed) forceSpawn = true;
                        }

                        bool randomSpawn = Random.value <= childBlueprint.eventChance;

                        if ((forceSpawn || randomSpawn) && eLimit.currentCount < eLimit.maxAllowed)
                        {
                            effectiveEventType = childBlueprint.eventType; 
                        }
                        eLimit.remainingCandidates--; 
                    }
                }

                List<GameObject> pool = GetPoolForEffectiveType(effectiveRoomType, effectiveEventType);
                
                // --- LOG QUAN TRỌNG 1: RỔ PREFAB TRỐNG ---
                if (pool == null || pool.Count == 0) 
                {
                    Debug.Log($"[GraphLevelGenerator] MISSING PREFAB: Rổ chứa cho phòng '{effectiveRoomType}' (Event: {effectiveEventType}) đang trống! Nhánh bị cắt đứt tại Node '{childBlueprint.nodeName}'.");
                    continue;
                }

                List<int> futureActiveChildren = _activeGraph.ContainsKey(childIndex) ? _activeGraph[childIndex] : new List<int>();

                Room nextRoom = TrySnapRoom(currentRoom, pool, childBlueprint, futureActiveChildren, effectiveRoomType);                
                
                // --- LOG QUAN TRỌNG 2: SẬP NHÁNH ---
                if (nextRoom == null) 
                {
                    Debug.Log($"[GraphLevelGenerator] BRANCH FAILED: Không thể đặt Node '{childBlueprint.nodeName}'. Tất cả các cửa đều bị đè map hoặc không khớp hướng!");
                    continue;
                }

                if (rLimit != null) rLimit.currentCount++;
                if (effectiveEventType != RoomEventType.None)
                {
                    EventTypeLimit eLimit = eventTypeLimits.Find(x => x.eventType == effectiveEventType);
                    if (eLimit != null) eLimit.currentCount++;
                }

                queue.Enqueue(new NodeTask { room = nextRoom, nodeIndex = childIndex, blueprint = childBlueprint });
            }
        }
        
        RuntimeTilemapMerger.Instance.MergeAllRooms(_spawnedRooms);
        Debug.Log($"[GraphLevelGenerator] Generation complete. Placed {_spawnedRooms.Count} rooms.");
        NotifyGenerationComplete();
    }

    public override Transform GetPlayerSpawnPoint()
    {
        if (_playerSpawnPoint != null) return _playerSpawnPoint;
        GameObject fallback = new GameObject("FallbackPlayerSpawn");
        fallback.transform.position = Vector3.zero;
        return fallback.transform;
    }

    private Room SpawnStartRoom()
    {
        List<GameObject> pool = GetPoolForEffectiveType(RoomNodeType.Start, RoomEventType.None);
        if (pool == null || pool.Count == 0) return null;
        GameObject prefab = pool[Random.Range(0, pool.Count)];
        GameObject startObj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        startObj.name = "Room_Start_0";
        Room room = startObj.GetComponent<Room>();
        if (room == null) return null;
        Transform spawnPt = startObj.transform.Find("PlayerSpawnPoint");
        _playerSpawnPoint = spawnPt != null ? spawnPt : startObj.transform;
        RegisterRoom(startObj, room);
        return room;
    }

    private Room TrySnapRoom(Room previousRoom, List<GameObject> pool, NodeBlueprint candidateBlueprint, List<int> futureActiveChildren, RoomNodeType effectiveRoomType)
    {
        bool forceDirection = candidateBlueprint.forceDirection;
        ExitDirection requiredDirFromParent = candidateBlueprint.requiredDir;
        int requiredChildrenCount = futureActiveChildren.Count;

        List<ExitDirection> mandatoryFutureExits = new List<ExitDirection>();
        foreach (int childIdx in futureActiveChildren)
        {
            NodeBlueprint futureChild = levelBlueprint.nodes[childIdx];
            if (futureChild.forceDirection) mandatoryFutureExits.Add(futureChild.requiredDir);
        }

        List<RoomExit> availableExits = new List<RoomExit>();
        foreach (var exit in previousRoom.Exits)
        {
            if (forceDirection) { if (exit.direction == requiredDirFromParent) availableExits.Add(exit); }
            else { availableExits.Add(exit); }
        }

        ShuffleList(availableExits);

        if (requiredChildrenCount == 0 && effectiveRoomType != RoomNodeType.Start)
        {
            pool = GetPoolForEffectiveType(RoomNodeType.DeadEnd, RoomEventType.None);
        }

        List<int> prefabIndices = new List<int>();
        for (int i = 0; i < pool.Count; i++) prefabIndices.Add(i);

        foreach (RoomExit exitA in availableExits)
        {
            ExitDirection requiredEntranceDir = Room.GetOpposite(exitA.direction);
            ShuffleList(prefabIndices);
            
            prefabIndices = prefabIndices.OrderBy(idx => {
                GameObject p = pool[idx];
                return _prefabUsageCount.ContainsKey(p) ? _prefabUsageCount[p] : 0;
            }).ToList();

            foreach (int idx in prefabIndices)
            {
                GameObject prefab = pool[idx];
                if (prefab == null) continue;
                Room prefabRoom = prefab.GetComponent<Room>();
                if (prefabRoom == null || prefabRoom.Exits.Count != requiredChildrenCount + 1) continue;
                if (!prefabRoom.TryGetExitInDirection(requiredEntranceDir, out _)) continue;

                bool hasAllMandatoryExits = true;
                foreach (ExitDirection reqDir in mandatoryFutureExits)
                {
                    if (reqDir == requiredEntranceDir || !prefabRoom.TryGetExitInDirection(reqDir, out _))
                    {
                        hasAllMandatoryExits = false;
                        break;
                    }
                }
                if (!hasAllMandatoryExits) continue;
                
                GameObject candidate = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                Room candidateRoom = candidate.GetComponent<Room>();

                if (!candidateRoom.TryGetExitInDirection(requiredEntranceDir, out RoomExit entranceB))
                {
                    Destroy(candidate); continue;
                }
                if (exitA.exitPoint == null || entranceB.exitPoint == null)
                {
                    Debug.Log($"[GraphLevelGenerator] LỖI TRANSFORM: Prefab {prefab.name} thiếu điểm neo ExitPoint!");
                    Destroy(candidate); continue;
                }

                Vector3 offset = exitA.exitPoint.position - entranceB.exitPoint.position;
                candidate.transform.position += offset;

                // --- LOG QUAN TRỌNG 3: TRUY VẾT TÊN PHÒNG BỊ ĐÈ MAP ---
                Room hitRoom = GetOverlappingRoom(GetRoomBounds(candidateRoom), previousRoom);
                if (hitRoom != null)
                {
                    Debug.Log($"[GraphLevelGenerator] OVERLAP: Thử ráp Node '{candidateBlueprint.nodeName}' (Prefab: {prefab.name}) nhưng bị đâm xuyên vào phòng '{hitRoom.gameObject.name}'. Đang thử phòng khác...");
                    Destroy(candidate); continue;
                }

                candidate.name = $"Room_{_spawnedRooms.Count}_{candidateBlueprint.nodeName}";
                previousRoom.RemoveExit(exitA);
                candidateRoom.RemoveExit(entranceB);
                RegisterRoom(candidate, candidateRoom);

                EnemySpawner[] spawners = candidate.GetComponentsInChildren<EnemySpawner>();
                foreach (EnemySpawner spawner in spawners) spawner.Init();

                if (_prefabUsageCount.ContainsKey(prefab)) _prefabUsageCount[prefab]++;
                else _prefabUsageCount[prefab] = 1;

                return candidateRoom;
            }
        }
        return null;
    }

    private Room GetOverlappingRoom(Bounds newBounds, Room parentRoom)
    {
        Bounds shrunkNew = new Bounds(newBounds.center, newBounds.size * shrinkFactor);
        foreach (Room existingRoom in _placedRoomComponents)
        {
            if (existingRoom == parentRoom) continue; 
            Bounds existingBounds = GetRoomBounds(existingRoom);
            Bounds shrunkExisting = new Bounds(existingBounds.center, existingBounds.size * shrinkFactor);
            if (shrunkNew.Intersects(shrunkExisting)) return existingRoom;
        }
        return null;
    }

    private Bounds GetRoomBounds(Room room)
    {
        UnityEngine.Tilemaps.TilemapRenderer[] renderers = room.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>();
        bool foundGround = false;
        Bounds combined = new Bounds(room.transform.position, Vector3.zero);

        foreach (var r in renderers)
        {
            if (r.gameObject.name == "Ground")
            {
                r.GetComponent<UnityEngine.Tilemaps.Tilemap>().CompressBounds();
                if (!foundGround) { combined = r.bounds; foundGround = true; }
                else { combined.Encapsulate(r.bounds); }
            }
        }
        if (foundGround) return combined;

        Collider2D col = room.GetComponent<Collider2D>();
        if (col != null) return col.bounds;
        return new Bounds(room.transform.position, new Vector3(20f, 12f, 1f));
    }

    private List<GameObject> GetPoolForEffectiveType(RoomNodeType roomType, RoomEventType eventType)
    {
        switch (eventType)
        {
            case RoomEventType.Statue: return statueRoomPrefabs;
            case RoomEventType.Reward: return rewardRoomPrefabs;
            case RoomEventType.Elite: return eliteRoomPrefabs;
        }

        return roomType switch
        {
            RoomNodeType.Start   => startRoomPrefabs,
            RoomNodeType.Normal  => normalRoomPrefabs,
            RoomNodeType.Statue  => statueRoomPrefabs,
            RoomNodeType.Reward  => rewardRoomPrefabs,
            RoomNodeType.Elite   => eliteRoomPrefabs,
            RoomNodeType.Buff    => buffRoomPrefabs,
            RoomNodeType.DeadEnd => deadEndRoomPrefabs,
            RoomNodeType.Goal    => goalRoomPrefabs,
            _ => null
        };
    }

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
        _prefabUsageCount.Clear();
        _playerSpawnPoint = null;

        foreach (var r in roomTypeLimits) r.currentCount = 0;
        foreach (var e in eventTypeLimits) 
        {
            e.currentCount = 0;
            e.remainingCandidates = 0;
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void BuildActiveGraph()
    {
        _activeGraph.Clear();
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(0);

        if (levelBlueprint.nodes.Count > 0) CountEventCandidate(levelBlueprint.nodes[0]);

        while (queue.Count > 0)
        {
            int currentIdx = queue.Dequeue();
            NodeBlueprint currentBp = levelBlueprint.nodes[currentIdx];
            List<int> activeChildren = new List<int>();

            if (currentBp.childrenIndices != null)
            {
                foreach (int childIdx in currentBp.childrenIndices)
                {
                    if (childIdx < 0 || childIdx >= levelBlueprint.nodes.Count) continue;

                    NodeBlueprint childBp = levelBlueprint.nodes[childIdx];
                    
                    if (Random.value <= childBp.spawnChance)
                    {
                        activeChildren.Add(childIdx);
                        queue.Enqueue(childIdx);
                        CountEventCandidate(childBp);
                    }
                    else
                    {
                        Debug.Log($"[GraphLevelGenerator] Pre-filter: Node '{childBp.nodeName}' was skipped due to spawn chance.");
                    }
                }
            }
            _activeGraph[currentIdx] = activeChildren;
        }
    }

    private void CountEventCandidate(NodeBlueprint bp)
    {
        if (bp.eventType != RoomEventType.None)
        {
            var eLimit = eventTypeLimits.Find(x => x.eventType == bp.eventType);
            if (eLimit != null) eLimit.remainingCandidates++;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_placedBounds == null || _placedBounds.Count == 0) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        foreach (Bounds b in _placedBounds) Gizmos.DrawWireCube(b.center, b.size);
    }
#endif
}