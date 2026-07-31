using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RuntimeTilemapMerger : MonoBehaviour
{
    public static RuntimeTilemapMerger Instance { get; private set; }

    public event System.Action OnMergeComplete;
    public bool IsMerging { get; private set; }

    [Header("Global Tilemaps")]
    public Tilemap globalGroundTilemap;
    public Tilemap globalBackgroundTilemap;

    [Header("Auto-Fill Gaps")]
    public RuleTile fillGroundRuleTile;
    public RuleTile fillBackgroundRuleTile;
    public int fillPadding = 15;

    private void Awake()
    {
        Instance = this;
    }

    public void MergeAllRooms(List<GameObject> spawnedRooms)
    {
        if (IsMerging)
        {
            Debug.LogWarning("[RuntimeTilemapMerger] A merge is already in progress.");
            return;
        }

        if (spawnedRooms == null || globalGroundTilemap == null || globalBackgroundTilemap == null)
        {
            Debug.LogError("[RuntimeTilemapMerger] Merge aborted: spawned rooms and both global tilemaps are required.");
            return;
        }

        IsMerging = true;
        SimulationMode2D previousSimulationMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        StartCoroutine(MergeRoutine(spawnedRooms, previousSimulationMode));
    }

    private IEnumerator MergeRoutine(List<GameObject> spawnedRooms, SimulationMode2D previousSimulationMode)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        int bgLayer = LayerMask.NameToLayer("Background");
        int minimapLayer = LayerMask.NameToLayer("Minimap Background");

        Dictionary<Vector3Int, TileBase> groundTiles = new Dictionary<Vector3Int, TileBase>();
        Dictionary<Vector3Int, TileBase> backgroundTiles = new Dictionary<Vector3Int, TileBase>();
        HashSet<Vector3Int> globalRoomFootprint = new HashSet<Vector3Int>();
        List<GameObject> localTilemapObjects = new List<GameObject>();
        bool mergeSucceeded = false;
        Exception mergeException = null;

        try
        {
            foreach (GameObject roomObj in spawnedRooms)
            {
                try
                {
                    if (roomObj != null)
                    {
                        CollectRoomTiles(
                            roomObj,
                            groundLayer,
                            bgLayer,
                            minimapLayer,
                            groundTiles,
                            backgroundTiles,
                            globalRoomFootprint,
                            localTilemapObjects);
                    }
                }
                catch (Exception exception)
                {
                    mergeException = exception;
                    break;
                }

                yield return null;
            }

            if (mergeException == null)
            {
                try
                {
                    // Nothing in the scene is hidden until collection succeeds. Clear stale data
                    // from an earlier generation only immediately before committing this merge.
                    globalGroundTilemap.ClearAllTiles();
                    globalBackgroundTilemap.ClearAllTiles();

                    ApplyTiles(globalGroundTilemap, groundTiles);
                    ApplyTiles(globalBackgroundTilemap, backgroundTiles);

                    if (fillGroundRuleTile != null)
                    {
                        FillEmptyCells(globalGroundTilemap, globalGroundTilemap.cellBounds, globalRoomFootprint, fillGroundRuleTile);
                    }

                    if (fillBackgroundRuleTile != null)
                    {
                        BoundsInt backgroundFillBounds = EncapsulateBounds(
                            globalBackgroundTilemap.cellBounds,
                            globalGroundTilemap.cellBounds);
                        FillEmptyCells(globalBackgroundTilemap, backgroundFillBounds, globalRoomFootprint, fillBackgroundRuleTile);
                    }

                    globalGroundTilemap.RefreshAllTiles();
                    CompositeCollider2D comp = globalGroundTilemap.GetComponent<CompositeCollider2D>();
                    if (comp != null) comp.GenerateGeometry();
                    globalBackgroundTilemap.RefreshAllTiles();

                    foreach (GameObject tilemapObject in localTilemapObjects)
                    {
                        if (tilemapObject != null) tilemapObject.SetActive(false);
                    }

                    mergeSucceeded = true;
                    Debug.Log($"[RuntimeTilemapMerger] Merge complete: {groundTiles.Count} ground tiles and {backgroundTiles.Count} background tiles from {spawnedRooms.Count} rooms.");
                }
                catch (Exception exception)
                {
                    mergeException = exception;
                }
            }

            if (mergeException != null)
            {
                // Keep the original room tilemaps visible if the global commit fails.
                globalGroundTilemap.ClearAllTiles();
                globalBackgroundTilemap.ClearAllTiles();
                foreach (GameObject tilemapObject in localTilemapObjects)
                {
                    if (tilemapObject != null) tilemapObject.SetActive(true);
                }
                Debug.LogException(mergeException, this);
                Debug.LogError("[RuntimeTilemapMerger] Merge failed. Original room tilemaps were left visible.");
            }
        }
        finally
        {
            IsMerging = false;
            Physics2D.simulationMode = previousSimulationMode;
        }

        if (mergeSucceeded) OnMergeComplete?.Invoke();
    }

    private void CollectRoomTiles(
        GameObject roomObj,
        int groundLayer,
        int backgroundLayer,
        int minimapLayer,
        Dictionary<Vector3Int, TileBase> groundTiles,
        Dictionary<Vector3Int, TileBase> backgroundTiles,
        HashSet<Vector3Int> roomFootprint,
        List<GameObject> localTilemapObjects)
    {
        Tilemap[] localTilemaps = roomObj.GetComponentsInChildren<Tilemap>();

        foreach (Tilemap localTilemap in localTilemaps)
        {
            string tilemapName = localTilemap.gameObject.name;
            bool isGround = localTilemap.gameObject.layer == groundLayer ||
                            tilemapName.Equals("Ground", StringComparison.OrdinalIgnoreCase);
            bool isBackground = localTilemap.gameObject.layer == backgroundLayer ||
                                tilemapName.Equals("Background", StringComparison.OrdinalIgnoreCase);
            bool isMinimap = localTilemap.gameObject.layer == minimapLayer ||
                             tilemapName.IndexOf("Minimap", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isGround && !isBackground && !isMinimap) continue;

            foreach (Vector3Int localCellPosition in localTilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = localTilemap.GetTile(localCellPosition);
                if (tile == null) continue;

                Vector3 worldPosition = localTilemap.GetCellCenterWorld(localCellPosition);

                if (isGround)
                {
                    Vector3Int groundCellPosition = globalGroundTilemap.WorldToCell(worldPosition);
                    groundCellPosition.z = 0;
                    groundTiles[groundCellPosition] = tile;
                    roomFootprint.Add(groundCellPosition);
                }

                if (isBackground)
                {
                    Vector3Int backgroundCellPosition = globalBackgroundTilemap.WorldToCell(worldPosition);
                    backgroundCellPosition.z = 0;
                    backgroundTiles[backgroundCellPosition] = tile;
                    roomFootprint.Add(backgroundCellPosition);
                }

                if (isMinimap)
                {
                    Vector3Int footprintCellPosition = globalGroundTilemap.WorldToCell(worldPosition);
                    footprintCellPosition.z = 0;
                    roomFootprint.Add(footprintCellPosition);
                }
            }

            if (isGround || isBackground) localTilemapObjects.Add(localTilemap.gameObject);
        }
    }

    private static void ApplyTiles(Tilemap tilemap, Dictionary<Vector3Int, TileBase> tiles)
    {
        foreach (KeyValuePair<Vector3Int, TileBase> entry in tiles)
        {
            tilemap.SetTile(entry.Key, entry.Value);
        }
    }

    private void FillEmptyCells(Tilemap tilemap, BoundsInt bounds, HashSet<Vector3Int> roomFootprint, TileBase fillTile)
    {
        int startX = bounds.xMin - fillPadding;
        int endX = bounds.xMax + fillPadding;
        int startY = bounds.yMin - fillPadding;
        int endY = bounds.yMax + fillPadding;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (!roomFootprint.Contains(position) && !tilemap.HasTile(position))
                {
                    tilemap.SetTile(position, fillTile);
                }
            }
        }
    }

    private static BoundsInt EncapsulateBounds(BoundsInt first, BoundsInt second)
    {
        if (first.size.x == 0 || first.size.y == 0) return second;
        if (second.size.x == 0 || second.size.y == 0) return first;

        int xMin = Mathf.Min(first.xMin, second.xMin);
        int yMin = Mathf.Min(first.yMin, second.yMin);
        int xMax = Mathf.Max(first.xMax, second.xMax);
        int yMax = Mathf.Max(first.yMax, second.yMax);
        return new BoundsInt(xMin, yMin, 0, xMax - xMin, yMax - yMin, 1);
    }
}
