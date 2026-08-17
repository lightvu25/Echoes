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
    [SerializeField] private Tilemap globalGroundColliderTilemap;

    [Header("Auto-Fill Gaps")]
    public RuleTile fillGroundRuleTile;
    public RuleTile fillBackgroundRuleTile;
    [SerializeField] private TileBase fillGroundColliderTile;
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
        bool useSeparatedGround = AllRoomsHaveSeparatedGround(spawnedRooms);

        if (useSeparatedGround && EnsureGlobalGroundColliderTilemap() == null)
        {
            IsMerging = false;
            Debug.LogError("[RuntimeTilemapMerger] Merge aborted: could not create the global Ground Collider tilemap.");
            return;
        }

        ConfigureGlobalGroundPhysics(useSeparatedGround);
        SyncGlobalRendererSettings(spawnedRooms);

        if (!useSeparatedGround)
        {
            Debug.LogWarning(
                "[RuntimeTilemapMerger] Not every spawned room has a Ground Collider tilemap. " +
                "Using the legacy combined visual/physics ground merge for this level.");
        }

        SimulationMode2D previousSimulationMode = Physics2D.simulationMode;
        Physics2D.simulationMode = SimulationMode2D.Script;
        StartCoroutine(MergeRoutine(spawnedRooms, previousSimulationMode, useSeparatedGround));
    }

    private IEnumerator MergeRoutine(
        List<GameObject> spawnedRooms,
        SimulationMode2D previousSimulationMode,
        bool useSeparatedGround)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        int bgLayer = LayerMask.NameToLayer("Background");
        int minimapLayer = LayerMask.NameToLayer("Minimap Background");

        Dictionary<Vector3Int, TileBase> groundVisualTiles = new Dictionary<Vector3Int, TileBase>();
        Dictionary<Vector3Int, TileBase> groundColliderTiles = new Dictionary<Vector3Int, TileBase>();
        Dictionary<Vector3Int, TileBase> backgroundTiles = new Dictionary<Vector3Int, TileBase>();
        HashSet<Vector3Int> groundFillFootprint = new HashSet<Vector3Int>();
        HashSet<Vector3Int> backgroundFillFootprint = new HashSet<Vector3Int>();
        HashSet<GameObject> localTilemapObjects = new HashSet<GameObject>();
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
                            groundVisualTiles,
                            groundColliderTiles,
                            backgroundTiles,
                            groundFillFootprint,
                            backgroundFillFootprint,
                            localTilemapObjects,
                            useSeparatedGround);
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
                    ValidateCollectedTiles(
                        groundVisualTiles,
                        groundColliderTiles,
                        backgroundTiles,
                        useSeparatedGround);

                    // Nothing in the scene is hidden until collection succeeds. Clear stale data
                    // from an earlier generation only immediately before committing this merge.
                    globalGroundTilemap.ClearAllTiles();
                    globalBackgroundTilemap.ClearAllTiles();
                    if (globalGroundColliderTilemap != null)
                        globalGroundColliderTilemap.ClearAllTiles();

                    ApplyTiles(globalGroundTilemap, groundVisualTiles);
                    ApplyTiles(globalBackgroundTilemap, backgroundTiles);
                    if (useSeparatedGround)
                        ApplyTiles(globalGroundColliderTilemap, groundColliderTiles);

                    if (fillGroundRuleTile != null)
                    {
                        FillEmptyCells(globalGroundTilemap, globalGroundTilemap.cellBounds, groundFillFootprint, fillGroundRuleTile);
                    }

                    if (useSeparatedGround && fillGroundColliderTile != null)
                    {
                        FillEmptyCells(
                            globalGroundColliderTilemap,
                            globalGroundTilemap.cellBounds,
                            groundFillFootprint,
                            fillGroundColliderTile);
                    }

                    if (fillBackgroundRuleTile != null)
                    {
                        BoundsInt backgroundFillBounds = EncapsulateBounds(
                            globalBackgroundTilemap.cellBounds,
                            globalGroundTilemap.cellBounds);
                        FillEmptyCells(globalBackgroundTilemap, backgroundFillBounds, backgroundFillFootprint, fillBackgroundRuleTile);
                    }

                    globalGroundTilemap.RefreshAllTiles();
                    globalBackgroundTilemap.RefreshAllTiles();

                    Tilemap physicsTilemap = useSeparatedGround
                        ? globalGroundColliderTilemap
                        : globalGroundTilemap;
                    if (physicsTilemap != null)
                    {
                        physicsTilemap.RefreshAllTiles();
                        TilemapCollider2D tilemapCollider = physicsTilemap.GetComponent<TilemapCollider2D>();
                        if (tilemapCollider != null) tilemapCollider.ProcessTilemapChanges();

                        CompositeCollider2D composite = physicsTilemap.GetComponent<CompositeCollider2D>();
                        if (composite != null) composite.GenerateGeometry();
                    }

                    foreach (GameObject tilemapObject in localTilemapObjects)
                    {
                        if (tilemapObject != null) tilemapObject.SetActive(false);
                    }

                    mergeSucceeded = true;
                    string groundMode = useSeparatedGround ? "separated visual/physics" : "combined visual/physics";
                    Debug.Log(
                        $"[RuntimeTilemapMerger] Merge complete ({groundMode}): " +
                        $"{groundVisualTiles.Count} ground visual tiles, " +
                        $"{groundColliderTiles.Count} ground collider tiles, " +
                        $"and {backgroundTiles.Count} background tiles " +
                        $"from {spawnedRooms.Count} rooms.");
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
                if (globalGroundColliderTilemap != null)
                    globalGroundColliderTilemap.ClearAllTiles();
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

    private static void ValidateCollectedTiles(
        Dictionary<Vector3Int, TileBase> groundVisualTiles,
        Dictionary<Vector3Int, TileBase> groundColliderTiles,
        Dictionary<Vector3Int, TileBase> backgroundTiles,
        bool useSeparatedGround)
    {
        if (groundVisualTiles.Count == 0)
            throw new InvalidOperationException("No visible Ground tiles were collected from the spawned rooms.");

        if (backgroundTiles.Count == 0)
            throw new InvalidOperationException("No Background tiles were collected from the spawned rooms.");

        if (useSeparatedGround && groundColliderTiles.Count == 0)
            throw new InvalidOperationException("Separated mode was selected, but no Ground Collider tiles were collected.");
    }

    private void CollectRoomTiles(
        GameObject roomObj,
        int groundLayer,
        int backgroundLayer,
        int minimapLayer,
        Dictionary<Vector3Int, TileBase> groundVisualTiles,
        Dictionary<Vector3Int, TileBase> groundColliderTiles,
        Dictionary<Vector3Int, TileBase> backgroundTiles,
        HashSet<Vector3Int> groundFillFootprint,
        HashSet<Vector3Int> backgroundFillFootprint,
        HashSet<GameObject> localTilemapObjects,
        bool useSeparatedGround)
    {
        Tilemap[] localTilemaps = roomObj.GetComponentsInChildren<Tilemap>();

        foreach (Tilemap localTilemap in localTilemaps)
        {
            string tilemapName = localTilemap.gameObject.name;
            bool isGroundCollider = IsGroundColliderName(tilemapName);
            bool isGroundVisual = IsGroundVisualName(tilemapName) ||
                                  (!useSeparatedGround &&
                                   localTilemap.gameObject.layer == groundLayer &&
                                   !isGroundCollider);
            bool isBackground = localTilemap.gameObject.layer == backgroundLayer ||
                                tilemapName.Equals("Background", StringComparison.OrdinalIgnoreCase);
            bool isMinimap = localTilemap.gameObject.layer == minimapLayer ||
                             tilemapName.IndexOf("Minimap", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isGroundVisual && !isGroundCollider && !isBackground && !isMinimap) continue;

            foreach (Vector3Int localCellPosition in localTilemap.cellBounds.allPositionsWithin)
            {
                TileBase tile = localTilemap.GetTile(localCellPosition);
                if (tile == null) continue;

                Vector3 worldPosition = localTilemap.GetCellCenterWorld(localCellPosition);

                if (isGroundVisual)
                {
                    Vector3Int groundCellPosition = globalGroundTilemap.WorldToCell(worldPosition);
                    groundCellPosition.z = 0;
                    groundVisualTiles[groundCellPosition] = tile;
                    groundFillFootprint.Add(groundCellPosition);
                    backgroundFillFootprint.Add(groundCellPosition);
                }

                if (useSeparatedGround && isGroundCollider)
                {
                    Vector3Int colliderCellPosition = globalGroundColliderTilemap.WorldToCell(worldPosition);
                    colliderCellPosition.z = 0;
                    groundColliderTiles[colliderCellPosition] = tile;
                }

                if (isBackground)
                {
                    Vector3Int backgroundCellPosition = globalBackgroundTilemap.WorldToCell(worldPosition);
                    backgroundCellPosition.z = 0;
                    backgroundTiles[backgroundCellPosition] = tile;
                    // Background art often extends beneath a room's floor. It
                    // must block background auto-fill, but must not block ground
                    // auto-fill or adjacent rooms can expose rectangular wall
                    // patches below otherwise continuous platforms.
                    backgroundFillFootprint.Add(backgroundCellPosition);
                }

                if (isMinimap)
                {
                    Vector3Int footprintCellPosition = globalGroundTilemap.WorldToCell(worldPosition);
                    footprintCellPosition.z = 0;
                    groundFillFootprint.Add(footprintCellPosition);
                    backgroundFillFootprint.Add(footprintCellPosition);
                }
            }

            if (isGroundVisual || isGroundCollider || isBackground)
                localTilemapObjects.Add(localTilemap.gameObject);
        }
    }

    private Tilemap EnsureGlobalGroundColliderTilemap()
    {
        if (globalGroundColliderTilemap != null && globalGroundColliderTilemap != globalGroundTilemap)
        {
            globalGroundColliderTilemap.gameObject.SetActive(true);
            return globalGroundColliderTilemap;
        }

        if (globalGroundTilemap == null) return null;

        GameObject colliderObject = new GameObject("Global Ground Collider (Runtime)");
        Transform sourceTransform = globalGroundTilemap.transform;
        Transform colliderTransform = colliderObject.transform;
        colliderTransform.SetParent(sourceTransform.parent, false);
        colliderTransform.localPosition = sourceTransform.localPosition;
        colliderTransform.localRotation = sourceTransform.localRotation;
        colliderTransform.localScale = sourceTransform.localScale;
        colliderObject.layer = globalGroundTilemap.gameObject.layer;

        globalGroundColliderTilemap = colliderObject.AddComponent<Tilemap>();
        globalGroundColliderTilemap.tileAnchor = globalGroundTilemap.tileAnchor;
        globalGroundColliderTilemap.orientation = globalGroundTilemap.orientation;

        TilemapRenderer renderer = colliderObject.AddComponent<TilemapRenderer>();
        renderer.enabled = false;

        Rigidbody2D body = colliderObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;

        colliderObject.AddComponent<CompositeCollider2D>();
        TilemapCollider2D tilemapCollider = colliderObject.AddComponent<TilemapCollider2D>();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        return globalGroundColliderTilemap;
    }

    private void ConfigureGlobalGroundPhysics(bool useSeparatedGround)
    {
        TilemapRenderer visualRenderer = globalGroundTilemap.GetComponent<TilemapRenderer>();
        if (visualRenderer != null) visualRenderer.enabled = true;

        TilemapCollider2D visualTilemapCollider = globalGroundTilemap.GetComponent<TilemapCollider2D>();
        if (visualTilemapCollider != null) visualTilemapCollider.enabled = !useSeparatedGround;

        CompositeCollider2D visualComposite = globalGroundTilemap.GetComponent<CompositeCollider2D>();
        if (visualComposite != null) visualComposite.enabled = !useSeparatedGround;

        Rigidbody2D visualBody = globalGroundTilemap.GetComponent<Rigidbody2D>();
        if (visualBody != null) visualBody.simulated = !useSeparatedGround;

        if (globalGroundColliderTilemap != null && globalGroundColliderTilemap != globalGroundTilemap)
            globalGroundColliderTilemap.gameObject.SetActive(useSeparatedGround);
    }

    private void SyncGlobalRendererSettings(List<GameObject> spawnedRooms)
    {
        Tilemap groundSource = null;
        Tilemap backgroundSource = null;

        foreach (GameObject roomObject in spawnedRooms)
        {
            if (roomObject == null) continue;

            Tilemap[] tilemaps = roomObject.GetComponentsInChildren<Tilemap>(true);
            foreach (Tilemap tilemap in tilemaps)
            {
                if (groundSource == null && IsGroundVisualName(tilemap.gameObject.name))
                    groundSource = tilemap;

                if (backgroundSource == null &&
                    tilemap.gameObject.name.Equals("Background", StringComparison.OrdinalIgnoreCase))
                    backgroundSource = tilemap;

                if (groundSource != null && backgroundSource != null) break;
            }

            if (groundSource != null && backgroundSource != null) break;
        }

        CopyRendererAppearance(groundSource, globalGroundTilemap);
        CopyRendererAppearance(backgroundSource, globalBackgroundTilemap);
    }

    private static void CopyRendererAppearance(Tilemap source, Tilemap destination)
    {
        if (source == null || destination == null) return;

        TilemapRenderer sourceRenderer = source.GetComponent<TilemapRenderer>();
        TilemapRenderer destinationRenderer = destination.GetComponent<TilemapRenderer>();
        if (sourceRenderer == null || destinationRenderer == null) return;

        destinationRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        destination.color = source.color;
        destination.tileAnchor = source.tileAnchor;
    }

    private static void ApplyTiles(Tilemap tilemap, Dictionary<Vector3Int, TileBase> tiles)
    {
        if (tiles.Count == 0) return;

        Vector3Int[] positions = new Vector3Int[tiles.Count];
        TileBase[] tileAssets = new TileBase[tiles.Count];
        int index = 0;

        foreach (KeyValuePair<Vector3Int, TileBase> entry in tiles)
        {
            positions[index] = entry.Key;
            tileAssets[index] = entry.Value;
            index++;
        }

        // A single batch prevents TilemapCollider2D and RuleTile neighbour refreshes
        // from being queued thousands of times during procedural generation.
        tilemap.SetTiles(positions, tileAssets);
    }

    private void FillEmptyCells(Tilemap tilemap, BoundsInt bounds, HashSet<Vector3Int> roomFootprint, TileBase fillTile)
    {
        int startX = bounds.xMin - fillPadding;
        int endX = bounds.xMax + fillPadding;
        int startY = bounds.yMin - fillPadding;
        int endY = bounds.yMax + fillPadding;
        List<Vector3Int> fillPositions = new List<Vector3Int>();

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (!roomFootprint.Contains(position) && !tilemap.HasTile(position))
                {
                    fillPositions.Add(position);
                }
            }
        }

        if (fillPositions.Count == 0) return;

        TileBase[] fillTiles = new TileBase[fillPositions.Count];
        Array.Fill(fillTiles, fillTile);
        tilemap.SetTiles(fillPositions.ToArray(), fillTiles);
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

    private static bool AllRoomsHaveSeparatedGround(List<GameObject> spawnedRooms)
    {
        bool foundGroundVisual = false;

        foreach (GameObject roomObject in spawnedRooms)
        {
            if (roomObject == null) continue;

            bool roomHasGroundVisual = false;
            bool roomHasGroundCollider = false;
            Tilemap[] roomTilemaps = roomObject.GetComponentsInChildren<Tilemap>(true);

            foreach (Tilemap tilemap in roomTilemaps)
            {
                string tilemapName = tilemap.gameObject.name;
                roomHasGroundVisual |= IsGroundVisualName(tilemapName);
                roomHasGroundCollider |= IsGroundColliderName(tilemapName);
            }

            if (roomHasGroundVisual)
            {
                foundGroundVisual = true;
                if (!roomHasGroundCollider) return false;
            }
        }

        return foundGroundVisual;
    }

    private static bool IsGroundVisualName(string objectName)
    {
        string normalized = NormalizeTilemapName(objectName);
        return normalized == "ground" || normalized == "tilemapground";
    }

    private static bool IsGroundColliderName(string objectName)
    {
        string normalized = NormalizeTilemapName(objectName);
        return normalized == "groundcollider" || normalized == "tilemapgroundcollider";
    }

    private static string NormalizeTilemapName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return string.Empty;

        char[] buffer = new char[objectName.Length];
        int length = 0;
        foreach (char character in objectName)
        {
            if (char.IsLetterOrDigit(character))
                buffer[length++] = char.ToLowerInvariant(character);
        }

        return new string(buffer, 0, length);
    }
}
