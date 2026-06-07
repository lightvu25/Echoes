using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AbyssLevelGenerator : BaseLevelGenerator
{
    // ------------------------------------------------------------------ //
    //  Inspector – Grid                                                    //
    // ------------------------------------------------------------------ //

    [Header("Grid")]
    [SerializeField] private int gridWidth  = 4;
    [SerializeField] private int gridHeight = 6;

    [Tooltip("World-space size of one grid cell (should match your room prefab bounds).")]
    [SerializeField] private Vector2 cellSize = new Vector2(20f, 12f);

    // ------------------------------------------------------------------ //
    //  Inspector – Room Prefabs                                            //
    // ------------------------------------------------------------------ //

    [Header("Room Prefabs — Normal (by biome depth)")]
    [Tooltip("Bottom rows (dark/narrow). Index 0 = deepest.")]
    [SerializeField] private List<GameObject> deepRoomPrefabs  = new List<GameObject>();
    [Tooltip("Top rows (wider/lighter).")]
    [SerializeField] private List<GameObject> upperRoomPrefabs = new List<GameObject>();

    [Header("Room Prefabs — Guaranteed")]
    [SerializeField] private GameObject startRoomPrefab;
    [SerializeField] private GameObject goalRoomPrefab;
    [SerializeField] private GameObject statueRoomPrefab;   // safe / mid-path
    [SerializeField] private GameObject chestRoomPrefab;    // dead-end branch
    [SerializeField] private GameObject dumpRoomPrefab;     // 2×2 horror room

    // ------------------------------------------------------------------ //
    //  Inspector – Dump Room                                               //
    // ------------------------------------------------------------------ //

    [Header("'The Dump' Room")]
    [Tooltip("How many grid cells wide The Dump occupies (≥ 2).")]
    [SerializeField] private int dumpWidth  = 2;
    [Tooltip("How many grid cells tall The Dump occupies (≥ 2).")]
    [SerializeField] private int dumpHeight = 2;

    // ------------------------------------------------------------------ //
    //  Inspector – Biome Threshold                                         //
    // ------------------------------------------------------------------ //

    [Header("Biome Split")]
    [Tooltip("Grid rows from the bottom that are considered 'deep' (use deepRoomPrefabs).")]
    [SerializeField] private int deepRowCount = 3;

    // ------------------------------------------------------------------ //
    //  Inspector – Branch Probability                                      //
    // ------------------------------------------------------------------ //

    [Header("Branching")]
    [Range(0f, 1f)]
    [SerializeField] private float branchChance = 0.4f;

    // ------------------------------------------------------------------ //
    //  Runtime State                                                       //
    // ------------------------------------------------------------------ //

    private enum CellType { Empty, Path, Branch, Start, Goal, Statue, Chest, Dump }

    private CellType[,] _grid;
    private RoomExitsMask[,] _requiredExits;
    private Vector2Int  _startCell;
    private Vector2Int  _goalCell;
    private Transform   _playerSpawnPoint;
    private readonly List<Vector2Int> _criticalPath = new List<Vector2Int>();

    // ------------------------------------------------------------------ //
    //  BaseLevelGenerator                                                  //
    // ------------------------------------------------------------------ //

    public override void GenerateMap(int levelNumber)
    {
        ClearPrevious();
        InitGrid();
        BuildCriticalPath();
        MarkStatueRoom();
        SpawnDumpRoom();
        SpawnBranches();
        CalculateRequiredExits();
        SpawnAllRooms();
        NotifyGenerationComplete();
    }

    public override Transform GetPlayerSpawnPoint() => _playerSpawnPoint;

    // ------------------------------------------------------------------ //
    //  Grid Initialisation                                                  //
    // ------------------------------------------------------------------ //

    private void InitGrid()
    {
        _grid = new CellType[gridWidth, gridHeight];
        // Start: random column on the bottom row (y = gridHeight - 1)
        _startCell = new Vector2Int(Random.Range(0, gridWidth), gridHeight - 1);
        _grid[_startCell.x, _startCell.y] = CellType.Start;
    }

    // ------------------------------------------------------------------ //
    //  Critical Path (Spelunky-style)                                      //
    // ------------------------------------------------------------------ //

    private void BuildCriticalPath()
    {
        _criticalPath.Clear();
        Vector2Int current = _startCell;
        _criticalPath.Add(current);

        while (current.y > 0)
        {
            // Decide next move: left, right, or up
            List<Vector2Int> candidates = new List<Vector2Int>();

            // Always allow moving up
            candidates.Add(new Vector2Int(current.x, current.y - 1));

            // Left / Right with bounds check — only offer if up is also possible
            // (Spelunky guarantees a left/right before each climb)
            if (current.x > 0)
                candidates.Add(new Vector2Int(current.x - 1, current.y));
            if (current.x < gridWidth - 1)
                candidates.Add(new Vector2Int(current.x + 1, current.y));

            Vector2Int next = candidates[Random.Range(0, candidates.Count)];

            // Prevent revisiting (simple guard; path is short so no full visited set needed)
            if (_grid[next.x, next.y] != CellType.Empty && next != _startCell)
            {
                // Force upward if we'd revisit
                next = new Vector2Int(current.x, current.y - 1);
            }

            if (_grid[next.x, next.y] == CellType.Empty)
                _grid[next.x, next.y] = CellType.Path;

            _criticalPath.Add(next);
            current = next;
        }

        // Goal is always the last cell reached at row 0
        _goalCell = current;
        _grid[_goalCell.x, _goalCell.y] = CellType.Goal;
    }

    // ------------------------------------------------------------------ //
    //  Statue Room (mid-point of critical path)                            //
    // ------------------------------------------------------------------ //

    private void MarkStatueRoom()
    {
        if (_criticalPath.Count < 3) return;
        int mid = _criticalPath.Count / 2;
        Vector2Int cell = _criticalPath[mid];
        // Don't overwrite start or goal
        if (_grid[cell.x, cell.y] == CellType.Path)
            _grid[cell.x, cell.y] = CellType.Statue;
    }

    // ------------------------------------------------------------------ //
    //  The Dump Room (2×2 block placed in the mid-depth region)           //
    // ------------------------------------------------------------------ //

    private Vector2Int _dumpOrigin = new Vector2Int(-1, -1);

    private void SpawnDumpRoom()
    {
        if (dumpRoomPrefab == null) return;

        // Find a region in mid-rows that doesn't overlap the critical path
        int rowStart = deepRowCount;                            // avoid the very bottom
        int rowEnd   = gridHeight - 1 - dumpHeight;            // avoid the start row
        int colEnd   = gridWidth - dumpWidth;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            int ox = Random.Range(0, colEnd + 1);
            int oy = Random.Range(rowStart, rowEnd + 1);

            if (DumpFits(ox, oy))
            {
                _dumpOrigin = new Vector2Int(ox, oy);
                for (int dx = 0; dx < dumpWidth; dx++)
                    for (int dy = 0; dy < dumpHeight; dy++)
                        _grid[ox + dx, oy + dy] = CellType.Dump;
                return;
            }
        }
        // No valid placement found; skip silently (level still valid)
    }

    private bool DumpFits(int ox, int oy)
    {
        if (ox + dumpWidth > gridWidth || oy + dumpHeight > gridHeight) return false;
        for (int dx = 0; dx < dumpWidth; dx++)
            for (int dy = 0; dy < dumpHeight; dy++)
                if (_grid[ox + dx, oy + dy] != CellType.Empty) return false;
        return true;
    }

    // ------------------------------------------------------------------ //
    //  Branch Dead-Ends                                                    //
    // ------------------------------------------------------------------ //

    private void SpawnBranches()
    {
        bool chestPlaced = false;

        foreach (Vector2Int cell in _criticalPath)
        {
            // Try each cardinal neighbour
            Vector2Int[] neighbours = {
                new Vector2Int(cell.x - 1, cell.y),
                new Vector2Int(cell.x + 1, cell.y),
                new Vector2Int(cell.x,     cell.y + 1),
                new Vector2Int(cell.x,     cell.y - 1),
            };

            foreach (Vector2Int n in neighbours)
            {
                if (!InBounds(n)) continue;
                if (_grid[n.x, n.y] != CellType.Empty) continue;
                if (Random.value > branchChance) continue;

                // First branch is always the guaranteed Chest room
                _grid[n.x, n.y] = (!chestPlaced) ? CellType.Chest : CellType.Branch;
                if (!chestPlaced) chestPlaced = true;
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  Instantiation                                                        //
    // ------------------------------------------------------------------ //

    private void SpawnAllRooms()
    {
        bool dumpSpawned = false;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                CellType type = _grid[x, y];
                if (type == CellType.Empty) continue;

                // The Dump is spawned once at its origin cell
                if (type == CellType.Dump)
                {
                    if (!dumpSpawned && x == _dumpOrigin.x && y == _dumpOrigin.y)
                    {
                        SpawnRoom(dumpRoomPrefab, x, y, isMultiCell: true);
                        dumpSpawned = true;
                    }
                    continue;
                }

                GameObject prefab = SelectPrefab(type, x, y);
                if (prefab == null) continue;

                GameObject room = SpawnRoom(prefab, x, y);

                if (type == CellType.Start)
                {
                    Transform spawnPt = room.transform.Find("PlayerSpawnPoint") ?? room.transform;
                    _playerSpawnPoint = spawnPt;
                }
            }
        }
    }

    private GameObject SelectPrefab(CellType type, int gx, int gy)
    {
        return type switch
        {
            CellType.Start  => startRoomPrefab,
            CellType.Goal   => goalRoomPrefab,
            CellType.Statue => statueRoomPrefab,
            CellType.Chest  => chestRoomPrefab,
            CellType.Dump   => dumpRoomPrefab,
            _               => PickBiomePrefab(gy, _requiredExits[gx, gy]),
        };
    }

    private GameObject PickBiomePrefab(int rowY, RoomExitsMask required)
    {
        int depthFromBottom = gridHeight - 1 - rowY;
        bool isDeep = depthFromBottom < deepRowCount;
        List<GameObject> pool = isDeep ? deepRoomPrefabs : upperRoomPrefabs;

        if (pool == null || pool.Count == 0) return null;
        return GetMatchingRoomPrefab(pool, required);
    }

    private GameObject SpawnRoom(GameObject prefab, int gx, int gy, bool isMultiCell = false)
    {
        Vector3 pos = GridToWorld(gx, gy);
        if (isMultiCell)
        {
            // Centre the multi-cell room over its footprint
            pos += new Vector3(
                (dumpWidth  - 1) * cellSize.x * 0.5f,
                -(dumpHeight - 1) * cellSize.y * 0.5f,
                0f);
        }

        GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);
        go.name = $"Room_{_grid[gx, gy]}_{gx}_{gy}";
        _spawnedRooms.Add(go);
        return go;
    }



    // ------------------------------------------------------------------ //
    //  Helpers                                                             //
    // ------------------------------------------------------------------ //

    private Vector3 GridToWorld(int gx, int gy)
    {
        // y=0 is top of the grid → negative world-Y is "up" here, so we flip.
        return transform.position + new Vector3(gx * cellSize.x, -gy * cellSize.y, 0f);
    }

    private bool InBounds(Vector2Int c) =>
        c.x >= 0 && c.x < gridWidth && c.y >= 0 && c.y < gridHeight;

    private void ClearPrevious()
    {
        ClearSpawnedRooms();
        _criticalPath.Clear();
        _playerSpawnPoint = null;
        _dumpOrigin = new Vector2Int(-1, -1);
    }

    private void CalculateRequiredExits()
    {
        _requiredExits = new RoomExitsMask[gridWidth, gridHeight];

        Vector2Int[] offsets = {
            new Vector2Int( 0, -1),
            new Vector2Int( 0,  1),
            new Vector2Int(-1,  0),
            new Vector2Int( 1,  0)
        };

        // y-1 = up in world, y+1 = down in world
        RoomExitsMask[] exitForOffset = {
            RoomExitsMask.Up,
            RoomExitsMask.Down,
            RoomExitsMask.Left,
            RoomExitsMask.Right
        };

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (_grid[x, y] == CellType.Empty) continue;

                RoomExitsMask mask = RoomExitsMask.None;
                for (int i = 0; i < offsets.Length; i++)
                {
                    Vector2Int neighbor = new Vector2Int(x + offsets[i].x, y + offsets[i].y);
                    if (!InBounds(neighbor)) continue;
                    if (_grid[neighbor.x, neighbor.y] == CellType.Empty) continue;
                    mask |= exitForOffset[i];
                }
                _requiredExits[x, y] = mask;
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  Editor Gizmos                                                       //
    // ------------------------------------------------------------------ //

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_grid == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Gizmos.color = _grid[x, y] switch
                {
                    CellType.Start  => Color.green,
                    CellType.Goal   => Color.yellow,
                    CellType.Statue => Color.cyan,
                    CellType.Chest  => new Color(1f, 0.6f, 0f),
                    CellType.Dump   => Color.red,
                    CellType.Path   => Color.white,
                    CellType.Branch => Color.gray,
                    _               => new Color(0, 0, 0, 0.1f),
                };
                Vector3 centre = GridToWorld(x, y);
                Gizmos.DrawWireCube(centre, new Vector3(cellSize.x * 0.9f, cellSize.y * 0.9f, 0f));

                Gizmos.color = Color.white;
                UnityEditor.Handles.Label(centre, _grid[x, y].ToString());
            }
        }
    }
#endif
}
