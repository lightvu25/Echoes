using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;

public enum RoomNodeType
{
    Start,
    Normal,
    DeadEnd,
    Goal
}

public class GraphLevelGenerator : BaseLevelGenerator
{
    public static GraphLevelGenerator Instance { get; private set; }

    [Header("Level Blueprints")]
    public List<LevelBlueprint> levelBlueprints = new List<LevelBlueprint>();

    private struct NodeTask
    {
        public Room room;
        public int nodeIndex;
        public NodeBlueprint blueprint;
    }

    [Header("Overlap Prevention")]
    [SerializeField] private float minimumRoomSpacing = 2f;
    [SerializeField] private float shrinkFactor = 0.55f;

    // --- Runtime State ---
    private readonly List<Bounds> _placedBounds = new List<Bounds>();
    private readonly List<Room> _placedRoomComponents = new List<Room>();
    private readonly Dictionary<int, List<int>> _activeGraph = new Dictionary<int, List<int>>();
    private readonly Dictionary<GameObject, int> _prefabUsageCount = new Dictionary<GameObject, int>();
    private Transform _playerSpawnPoint;
    
    // The current level data being used
    private LevelBlueprint _currentLevelData;

    public IReadOnlyList<Room> PlacedRooms => _placedRoomComponents;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public override void GenerateMap(int levelNumber)
    {
        if (levelBlueprints == null || levelBlueprints.Count == 0) return;
        
        int index = Mathf.Clamp(levelNumber - 1, 0, levelBlueprints.Count - 1);
        _currentLevelData = levelBlueprints[index];

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentLevelName = _currentLevelData.name.Replace(" Blueprint", "").Replace(" (Level Blueprint)", "").Trim();
        }

        ClearGraphState();
        if (_currentLevelData.nodes == null || _currentLevelData.nodes.Count == 0) return;

        BuildActiveGraph();

        Room startRoom = SpawnStartRoom();
        if (startRoom == null) return;

        Queue<NodeTask> queue = new Queue<NodeTask>();
        queue.Enqueue(new NodeTask { room = startRoom, nodeIndex = 0, blueprint = _currentLevelData.nodes[0] });

        // Core loop
        while (queue.Count > 0)
        {
            NodeTask currentTask = queue.Dequeue();
            Room currentRoom = currentTask.room;
            int currentIdx = currentTask.nodeIndex;

            if (!_activeGraph.TryGetValue(currentIdx, out List<int> activeChildren)) continue;

            activeChildren = activeChildren.OrderByDescending(idx => _currentLevelData.nodes[idx].forceDirection).ToList();
            
            foreach (int childIndex in activeChildren)
            {
                NodeBlueprint childBlueprint = _currentLevelData.nodes[childIndex];
                
                RoomNodeType effectiveRoomType = childBlueprint.roomType;

                RoomEventType effectiveEventType = RoomEventType.None;
                
                if (childBlueprint.eventType != RoomEventType.None)
                {
                    EventTypeLimit eLimit = _currentLevelData.eventTypeLimits.Find(x => x.eventType == childBlueprint.eventType);
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
                    else
                    {
                        // Fallback: If no global limit is defined, just use the node's individual chance
                        bool randomSpawn = Random.value <= childBlueprint.eventChance;
                        if (randomSpawn)
                        {
                            effectiveEventType = childBlueprint.eventType; 
                        }
                    }
                }

                List<GameObject> pool = GetPoolForEffectiveType(effectiveRoomType, effectiveEventType);
                
                // LOG 1
                if (pool == null || pool.Count == 0) 
                {
                    Debug.Log($"[GraphLevelGenerator] MISSING PREFAB: Rổ chứa cho phòng '{effectiveRoomType}' (Event: {effectiveEventType}) đang trống! Nhánh bị cắt đứt tại Node '{childBlueprint.nodeName}'.");
                    continue;
                }

                List<int> futureActiveChildren = _activeGraph.ContainsKey(childIndex) ? _activeGraph[childIndex] : new List<int>();

                Room nextRoom = TrySnapRoom(currentRoom, pool, childBlueprint, futureActiveChildren, effectiveRoomType, effectiveEventType);                
                
                // LOG 2
                if (nextRoom == null) 
                {
                    int requiredDoors = futureActiveChildren.Count + 1;
                    if (futureActiveChildren.Count == 0 && effectiveRoomType != RoomNodeType.Start) requiredDoors = 1; // DeadEnd needs 1 door

                    Debug.Log($"[GraphLevelGenerator] BRANCH FAILED: Không thể đặt Node '{childBlueprint.nodeName}' (Loại: {effectiveRoomType}).\n" +
                              $"Nguyên nhân: Không tìm thấy Prefab nào thỏa mãn ĐỒNG THỜI các điều kiện sau:\n" +
                              $"- Thuộc danh sách Prefab của loại phòng này.\n" +
                              $"- Có ĐÚNG {requiredDoors} cửa (Exits).\n" +
                              $"- Có cửa vào khớp với cửa ra của phòng trước.\n" +
                              $"- Không bị đè lên các phòng đã tạo trước đó.");
                    continue;
                }

                if (effectiveEventType != RoomEventType.None)
                {
                    nextRoom.AddEvent(effectiveEventType);
                    EventTypeLimit eLimit = _currentLevelData.eventTypeLimits.Find(x => x.eventType == effectiveEventType);
                    if (eLimit != null) eLimit.currentCount++;
                }

                // Process dynamic injections ONLY if there's no primary event
                if (effectiveEventType == RoomEventType.None && childBlueprint.dynamicInjections != null)
                {
                    foreach (var injection in childBlueprint.dynamicInjections)
                    {
                        if (Random.value <= injection.spawnChance)
                        {
                            InjectionTypeLimit iLimit = _currentLevelData.injectionLimits.Find(x => x.injectionType == injection.injectionType);
                            bool canSpawn = true;

                            // Global cap: don't exceed the maximum allowed count
                            if (iLimit != null && iLimit.currentCount >= iLimit.maxAllowed)
                            {
                                canSpawn = false;
                            }

                            // Neighbor rule: don't place the same injection type in adjacent rooms
                            if (canSpawn && currentRoom.HasInjection(injection.injectionType))
                            {
                                canSpawn = false;
                            }

                            if (canSpawn)
                            {
                                nextRoom.AddInjection(injection.injectionType);
                                if (iLimit != null) iLimit.currentCount++;
                            }
                        }
                    }
                }

                queue.Enqueue(new NodeTask { room = nextRoom, nodeIndex = childIndex, blueprint = childBlueprint });
            }
        }
        
        if (RuntimeLevelPopulator.Instance != null)
        {
            RuntimeLevelPopulator.Instance.PopulateRooms(_placedRoomComponents, _currentLevelData);
        }
        else
        {
            Debug.LogWarning("[GraphLevelGenerator] RuntimeLevelPopulator is missing in the scene!");
        }

        // Cache original bounds before TilemapMerger strips the tilemaps
        foreach (Room room in _placedRoomComponents) 
        {
            room.OriginalBounds = GetRoomBounds(room);
        }

        RuntimeTilemapMerger.Instance.MergeAllRooms(_spawnedRooms);
        Debug.Log($"[GraphLevelGenerator] Generation complete. Placed {_spawnedRooms.Count} rooms.");
        
        MapUI mapUI = MapUI.Instance;
        if (mapUI == null) mapUI = FindFirstObjectByType<MapUI>(FindObjectsInactive.Include);

        if (mapUI != null)
        {
            mapUI.InitializeMapGraph(_placedRoomComponents);
            if (_placedRoomComponents.Count > 0)
            {
                mapUI.RevealRoom(_placedRoomComponents[0]);
            }
        }

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

    private Room TrySnapRoom(Room previousRoom, List<GameObject> pool, NodeBlueprint candidateBlueprint, List<int> futureActiveChildren, RoomNodeType effectiveRoomType, RoomEventType effectiveEventType)
    {
        bool forceDirection = candidateBlueprint.forceDirection;
        ExitDirection requiredDirFromParent = candidateBlueprint.requiredDir;
        int requiredChildrenCount = futureActiveChildren.Count;

        List<ExitDirection> mandatoryFutureExits = new List<ExitDirection>();
        foreach (int childIdx in futureActiveChildren)
        {
            NodeBlueprint futureChild = _currentLevelData.nodes[childIdx];
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
            // IMPORTANT: Pass effectiveEventType instead of RoomEventType.None, 
            // so if this dead end is meant to be a Shop/Reward, it will pull from those prefabs!
            pool = GetPoolForEffectiveType(RoomNodeType.DeadEnd, effectiveEventType);
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

                int currentTier = 1;
                if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
                {
                    currentTier = GameSession.Instance.currentRun.levelNumber;
                }

                EnemySpawner[] spawners = candidate.GetComponentsInChildren<EnemySpawner>();
                foreach (EnemySpawner spawner in spawners) spawner.Init(currentTier);

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
        
        // Priority 1: Check for a dedicated Minimap Tilemap
        foreach (var r in renderers)
        {
            if (r.gameObject.layer == LayerMask.NameToLayer("Minimap Background") || r.gameObject.name.Contains("Minimap"))
            {
                r.GetComponent<UnityEngine.Tilemaps.Tilemap>().CompressBounds();
                if (r.bounds.size != Vector3.zero) return r.bounds;
            }
        }

        // Priority 2: Fallback to encapsulating all tilemaps
        bool foundBounds = false;
        Bounds combined = new Bounds(room.transform.position, Vector3.zero);

        foreach (var r in renderers)
        {
            r.GetComponent<UnityEngine.Tilemaps.Tilemap>().CompressBounds();
            if (r.bounds.size == Vector3.zero) continue;

            if (!foundBounds) { combined = r.bounds; foundBounds = true; }
            else { combined.Encapsulate(r.bounds); }
        }
        if (foundBounds) return combined;

        return new Bounds(room.transform.position, new Vector3(20f, 12f, 1f));
    }

    private List<GameObject> GetPoolForEffectiveType(RoomNodeType roomType, RoomEventType eventType)
    {
        switch (eventType)
        {

            case RoomEventType.Reward: return _currentLevelData.rewardRoomPrefabs;
            case RoomEventType.Elite: return _currentLevelData.eliteRoomPrefabs;
            case RoomEventType.Story: return _currentLevelData.storyRoomPrefabs;
            case RoomEventType.EchoRoom: return _currentLevelData.echoRoomPrefabs;
            case RoomEventType.CursedChest: return _currentLevelData.cursedChestRoomPrefabs;
            case RoomEventType.HighMagicFactor: return _currentLevelData.highMagicFactorRoomPrefabs;
            case RoomEventType.Shop: return _currentLevelData.shopRoomPrefabs;
        }

        return roomType switch
        {
            RoomNodeType.Start   => _currentLevelData.startRoomPrefabs,
            RoomNodeType.Normal  => _currentLevelData.normalRoomPrefabs,
            RoomNodeType.DeadEnd => _currentLevelData.deadEndRoomPrefabs,
            RoomNodeType.Goal    => _currentLevelData.goalRoomPrefabs,
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

        if (_currentLevelData != null)
        {
            foreach (var e in _currentLevelData.eventTypeLimits) 
            {
                e.currentCount = 0;
                e.remainingCandidates = 0;
            }
            foreach (var i in _currentLevelData.injectionLimits)
            {
                i.currentCount = 0;
                i.remainingCandidates = 0;
            }
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

        if (_currentLevelData.nodes.Count > 0) CountEventCandidate(_currentLevelData.nodes[0]);

        while (queue.Count > 0)
        {
            int currentIdx = queue.Dequeue();
            NodeBlueprint currentBp = _currentLevelData.nodes[currentIdx];
            List<int> activeChildren = new List<int>();

            if (currentBp.childrenIndices != null)
            {
                foreach (int childIdx in currentBp.childrenIndices)
                {
                    if (childIdx < 0 || childIdx >= _currentLevelData.nodes.Count) continue;

                    NodeBlueprint childBp = _currentLevelData.nodes[childIdx];
                    
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
            var eLimit = _currentLevelData.eventTypeLimits.Find(x => x.eventType == bp.eventType);
            if (eLimit != null) eLimit.remainingCandidates++;
        }

        if (bp.dynamicInjections != null)
        {
            foreach (var injection in bp.dynamicInjections)
            {
                var iLimit = _currentLevelData.injectionLimits.Find(x => x.injectionType == injection.injectionType);
                if (iLimit != null) iLimit.remainingCandidates++;
            }
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