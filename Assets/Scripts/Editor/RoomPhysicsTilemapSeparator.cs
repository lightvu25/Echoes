using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class RoomPhysicsTilemapSeparator : EditorWindow
{
    private const string DefaultRoomFolder = "Assets/Prefabs/Rooms";
    private const string DefaultGroundColliderTilePath = "Assets/Tilemap/Collider Tile.asset";
    private const string DefaultOneWayColliderTilePath = "Assets/Tilemap/OneWayPlatform Collider Tile.asset";
    private const string GroundColliderName = "Ground Collider";
    private const string OneWayColliderName = "OneWayPlatform Collider";

    [SerializeField] private string roomFolder = DefaultRoomFolder;
    [SerializeField] private TileBase groundColliderTile;
    [SerializeField] private TileBase oneWayColliderTile;
    [SerializeField] private bool hideColliderRenderers = true;
    [SerializeField] private bool dryRun = true;

    [MenuItem("Tools/Echoes/Separate Room Physics Tilemaps")]
    public static void ShowWindow()
    {
        GetWindow<RoomPhysicsTilemapSeparator>("Room Physics Separator");
    }

    [MenuItem("Tools/Echoes/Preview Room Physics Separation")]
    public static void PreviewAllRoomPrefabs()
    {
        TileBase groundTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultGroundColliderTilePath);
        TileBase oneWayTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultOneWayColliderTilePath);
        ProcessAllRoomPrefabs(DefaultRoomFolder, groundTile, oneWayTile, true, true);
    }

    private void OnEnable()
    {
        if (groundColliderTile == null)
            groundColliderTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultGroundColliderTilePath);

        if (oneWayColliderTile == null)
            oneWayColliderTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultOneWayColliderTilePath);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Room Visual / Physics Tilemap Separator", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "For every room prefab, this tool duplicates Ground and OneWayPlatform tilemaps into collider-only copies. " +
            "It replaces occupied cells with the selected collider tiles, keeps physics on the copies, and removes physics " +
            "from the visual originals. Running it again updates the existing copies instead of duplicating them.",
            MessageType.Info);

        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            EditorGUILayout.HelpBox(
                "A Prefab is currently open. Preview is safe, but close Prefab Mode before applying to avoid overwriting its open editing state.",
                MessageType.Warning);
        }

        EditorGUILayout.Space();
        roomFolder = EditorGUILayout.TextField("Room Prefab Folder", roomFolder);
        groundColliderTile = (TileBase)EditorGUILayout.ObjectField(
            "Ground Collider Tile", groundColliderTile, typeof(TileBase), false);
        oneWayColliderTile = (TileBase)EditorGUILayout.ObjectField(
            "One-Way Collider Tile", oneWayColliderTile, typeof(TileBase), false);
        hideColliderRenderers = EditorGUILayout.Toggle("Hide Collider Renderers", hideColliderRenderers);
        dryRun = EditorGUILayout.Toggle("Dry Run (Preview Only)", dryRun);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!InputsAreValid()))
        {
            if (GUILayout.Button(dryRun ? "Preview All Room Prefabs" : "Convert All Room Prefabs"))
            {
                if (!dryRun && PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    EditorUtility.DisplayDialog(
                        "Close Prefab Mode First",
                        "Close the currently opened room prefab, then run the conversion again.",
                        "OK");
                    return;
                }

                if (!dryRun && !EditorUtility.DisplayDialog(
                        "Separate Room Physics Tilemaps?",
                        "This will update every room prefab under the selected folder. Existing collider copies will be refreshed. " +
                        "The operation is safe to run repeatedly, but should still be committed through version control.",
                        "Convert",
                        "Cancel"))
                {
                    return;
                }

                ProcessAllRoomPrefabs(
                    roomFolder,
                    groundColliderTile,
                    oneWayColliderTile,
                    dryRun,
                    hideColliderRenderers);
            }
        }

        if (!InputsAreValid())
        {
            EditorGUILayout.HelpBox(
                "Assign both collider Tile assets and choose an existing room prefab folder.",
                MessageType.Error);
        }
    }

    private bool InputsAreValid()
    {
        return groundColliderTile != null &&
               oneWayColliderTile != null &&
               AssetDatabase.IsValidFolder(roomFolder);
    }

    private static void ProcessAllRoomPrefabs(
        string prefabFolder,
        TileBase groundTile,
        TileBase oneWayTile,
        bool previewOnly,
        bool hideRenderers)
    {
        if (groundTile == null || oneWayTile == null)
        {
            Debug.LogError("[RoomPhysicsSeparator] Both collider Tile assets are required.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            Debug.LogError($"[RoomPhysicsSeparator] Room prefab folder does not exist: {prefabFolder}");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
        ConversionSummary summary = new ConversionSummary(prefabGuids.Length, previewOnly);

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (EditorUtility.DisplayCancelableProgressBar(
                        previewOnly ? "Previewing Room Physics Separation" : "Separating Room Physics Tilemaps",
                        prefabPath,
                        prefabGuids.Length == 0 ? 1f : (float)i / prefabGuids.Length))
                {
                    summary.WasCancelled = true;
                    break;
                }

                ProcessPrefab(
                    prefabPath,
                    groundTile,
                    oneWayTile,
                    previewOnly,
                    hideRenderers,
                    summary);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (!previewOnly)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log(summary.BuildMessage());
    }

    private static void ProcessPrefab(
        string prefabPath,
        TileBase groundTile,
        TileBase oneWayTile,
        bool previewOnly,
        bool hideRenderers,
        ConversionSummary summary)
    {
        GameObject prefabRoot = null;

        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            Tilemap[] allTilemaps = prefabRoot.GetComponentsInChildren<Tilemap>(true);
            List<Tilemap> visualTilemaps = new List<Tilemap>();

            foreach (Tilemap tilemap in allTilemaps)
            {
                if (IsGroundVisualName(tilemap.gameObject.name) || IsOneWayVisualName(tilemap.gameObject.name))
                    visualTilemaps.Add(tilemap);
            }

            if (visualTilemaps.Count == 0)
            {
                summary.PrefabsWithoutTargets++;
                return;
            }

            bool prefabModified = false;

            foreach (Tilemap visualTilemap in visualTilemaps)
            {
                bool isOneWay = IsOneWayVisualName(visualTilemap.gameObject.name);
                TileBase replacementTile = isOneWay ? oneWayTile : groundTile;
                string colliderName = isOneWay ? OneWayColliderName : GroundColliderName;
                int occupiedCells = CountOccupiedCells(visualTilemap);

                if (isOneWay)
                    summary.OneWayVisualsFound++;
                else
                    summary.GroundVisualsFound++;

                summary.OccupiedCellsCopied += occupiedCells;

                GameObject colliderObject = FindColliderSibling(visualTilemap.transform, colliderName);
                bool willCreate = colliderObject == null;

                if (previewOnly)
                {
                    Debug.Log(
                        $"[RoomPhysicsSeparator][PREVIEW] {prefabPath}: " +
                        $"{visualTilemap.gameObject.name} -> {colliderName} " +
                        $"({occupiedCells} occupied cells, {(willCreate ? "create" : "update")}).");
                    continue;
                }

                if (willCreate)
                {
                    colliderObject = Instantiate(visualTilemap.gameObject, visualTilemap.transform.parent, false);
                    colliderObject.name = colliderName;
                    colliderObject.transform.SetSiblingIndex(visualTilemap.transform.GetSiblingIndex() + 1);
                    summary.ColliderObjectsCreated++;
                }
                else
                {
                    summary.ColliderObjectsUpdated++;
                }

                SynchronizeTransformAndIdentity(visualTilemap.gameObject, colliderObject);
                RemoveNonColliderComponents(colliderObject);

                Tilemap colliderTilemap = colliderObject.GetComponent<Tilemap>();
                if (colliderTilemap == null)
                    throw new InvalidOperationException($"{colliderName} in {prefabPath} has no Tilemap component.");

                CopyOccupiedCells(visualTilemap, colliderTilemap, replacementTile);
                ConfigurePhysics(colliderObject, isOneWay, hideRenderers);
                summary.PhysicsComponentsRemoved += RemovePhysicsFromVisual(visualTilemap.gameObject);
                prefabModified = true;
            }

            if (prefabModified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                summary.PrefabsModified++;
            }
        }
        catch (Exception exception)
        {
            summary.Failures++;
            Debug.LogError($"[RoomPhysicsSeparator] Failed to process {prefabPath}: {exception}");
        }
        finally
        {
            if (prefabRoot != null)
                PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static GameObject FindColliderSibling(Transform visualTransform, string colliderName)
    {
        Transform parent = visualTransform.parent;
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != visualTransform &&
                child.name.Equals(colliderName, StringComparison.OrdinalIgnoreCase) &&
                child.GetComponent<Tilemap>() != null)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static void SynchronizeTransformAndIdentity(GameObject visualObject, GameObject colliderObject)
    {
        Transform source = visualObject.transform;
        Transform target = colliderObject.transform;
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
        colliderObject.layer = visualObject.layer;
        colliderObject.SetActive(visualObject.activeSelf);
    }

    private static void CopyOccupiedCells(Tilemap source, Tilemap destination, TileBase replacementTile)
    {
        List<Vector3Int> occupiedPositions = new List<Vector3Int>();
        foreach (Vector3Int position in source.cellBounds.allPositionsWithin)
        {
            if (source.HasTile(position))
                occupiedPositions.Add(position);
        }

        destination.ClearAllTiles();

        if (occupiedPositions.Count > 0)
        {
            TileBase[] colliderTiles = new TileBase[occupiedPositions.Count];
            Array.Fill(colliderTiles, replacementTile);
            destination.SetTiles(occupiedPositions.ToArray(), colliderTiles);
        }

        destination.RefreshAllTiles();
        destination.CompressBounds();
    }

    private static int CountOccupiedCells(Tilemap tilemap)
    {
        int count = 0;
        foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(position)) count++;
        }

        return count;
    }

    private static void ConfigurePhysics(GameObject colliderObject, bool oneWay, bool hideRenderer)
    {
        Rigidbody2D body = colliderObject.GetComponent<Rigidbody2D>();
        if (body == null) body = colliderObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;

        CompositeCollider2D composite = colliderObject.GetComponent<CompositeCollider2D>();
        if (composite == null) composite = colliderObject.AddComponent<CompositeCollider2D>();

        TilemapCollider2D tilemapCollider = colliderObject.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null) tilemapCollider = colliderObject.AddComponent<TilemapCollider2D>();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        tilemapCollider.usedByEffector = oneWay;
        composite.usedByEffector = oneWay;

        PlatformEffector2D platformEffector = colliderObject.GetComponent<PlatformEffector2D>();
        if (oneWay)
        {
            if (platformEffector == null) platformEffector = colliderObject.AddComponent<PlatformEffector2D>();
            platformEffector.useOneWay = true;
            platformEffector.useOneWayGrouping = true;
            platformEffector.useSideFriction = false;
            platformEffector.useSideBounce = false;
        }
        else if (platformEffector != null)
        {
            DestroyImmediate(platformEffector);
        }

        TilemapRenderer renderer = colliderObject.GetComponent<TilemapRenderer>();
        if (renderer != null) renderer.enabled = !hideRenderer;

        tilemapCollider.ProcessTilemapChanges();
        composite.GenerateGeometry();
        EditorUtility.SetDirty(colliderObject);
    }

    private static int RemovePhysicsFromVisual(GameObject visualObject)
    {
        int removed = 0;

        Collider2D[] colliders = visualObject.GetComponents<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            DestroyImmediate(collider);
            removed++;
        }

        Effector2D[] effectors = visualObject.GetComponents<Effector2D>();
        foreach (Effector2D effector in effectors)
        {
            DestroyImmediate(effector);
            removed++;
        }

        Rigidbody2D body = visualObject.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            DestroyImmediate(body);
            removed++;
        }

        EditorUtility.SetDirty(visualObject);
        return removed;
    }

    private static void RemoveNonColliderComponents(GameObject colliderObject)
    {
        Component[] components = colliderObject.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null) continue;
            if (component is Transform ||
                component is Tilemap ||
                component is TilemapRenderer ||
                component is Rigidbody2D ||
                component is TilemapCollider2D ||
                component is CompositeCollider2D ||
                component is PlatformEffector2D)
            {
                continue;
            }

            DestroyImmediate(component);
        }
    }

    private static bool IsGroundVisualName(string objectName)
    {
        string normalized = NormalizeName(objectName);
        return normalized == "ground" || normalized == "tilemapground";
    }

    private static bool IsOneWayVisualName(string objectName)
    {
        string normalized = NormalizeName(objectName);
        return normalized == "onewayplatform" || normalized == "tilemaponewayplatform";
    }

    private static string NormalizeName(string objectName)
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

    private sealed class ConversionSummary
    {
        public readonly int PrefabsFound;
        public readonly bool PreviewOnly;
        public int PrefabsModified;
        public int PrefabsWithoutTargets;
        public int GroundVisualsFound;
        public int OneWayVisualsFound;
        public int ColliderObjectsCreated;
        public int ColliderObjectsUpdated;
        public int OccupiedCellsCopied;
        public int PhysicsComponentsRemoved;
        public int Failures;
        public bool WasCancelled;

        public ConversionSummary(int prefabsFound, bool previewOnly)
        {
            PrefabsFound = prefabsFound;
            PreviewOnly = previewOnly;
        }

        public string BuildMessage()
        {
            string mode = PreviewOnly ? "PREVIEW" : "COMPLETE";
            string cancellation = WasCancelled ? " (cancelled early)" : string.Empty;
            return
                $"[RoomPhysicsSeparator][{mode}]{cancellation} Prefabs found: {PrefabsFound}, " +
                $"modified: {PrefabsModified}, without targets: {PrefabsWithoutTargets}, " +
                $"Ground: {GroundVisualsFound}, OneWayPlatform: {OneWayVisualsFound}, " +
                $"collider objects created: {ColliderObjectsCreated}, updated: {ColliderObjectsUpdated}, " +
                $"occupied cells: {OccupiedCellsCopied}, physics components removed: {PhysicsComponentsRemoved}, " +
                $"failures: {Failures}.";
        }
    }
}
