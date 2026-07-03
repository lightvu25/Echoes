using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.Collections.Generic;

public class TilemapSortingLayerFixer : EditorWindow
{
    // Maps a child GameObject name to the Sorting Layer it should use.
    // Add or modify entries here to match your project's naming conventions.
    private static readonly Dictionary<string, string> nameToSortingLayer = new Dictionary<string, string>
    {
        { "Background",      "Background" },
        { "Ground",          "Ground" },
        { "OneWayPlatform",  "OneWayPlatform" },
        { "Foreground",      "Default" },
        { "Event",           "Default" },
    };

    // For child objects on specific Unity Layers (by layer index), override the sorting layer.
    // This catches objects whose names don't match the dictionary above.
    private static readonly Dictionary<int, string> layerToSortingLayer = new Dictionary<int, string>
    {
        { 10, "Background" },       // Unity Layer "Background"
        { 12, "MinimapBackground" }, // Unity Layer "Minimap Background"
        { 13, "OneWayPlatform" },    // Unity Layer "OneWayPlatform"
        { 3,  "Ground" },           // Unity Layer "Ground"
    };

    private Vector2 scrollPos;
    private string prefabFolder = "Assets/Prefabs/Rooms";
    private bool dryRun = true;

    [MenuItem("Tools/Echoes/Fix Tilemap Sorting Layers")]
    public static void ShowWindow()
    {
        GetWindow<TilemapSortingLayerFixer>("Tilemap Sorting Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tilemap Sorting Layer Fixer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool assigns each TilemapRenderer's Sorting Layer based on its GameObject name first, " +
            "then falls back to its Unity Layer.\n\n" +
            "Name Mapping (highest priority):\n" +
            "  Background → Background\n" +
            "  Ground → Ground\n" +
            "  OneWayPlatform → OneWayPlatform\n" +
            "  Foreground → Default\n" +
            "  Event → Default\n\n" +
            "Layer Mapping (fallback):\n" +
            "  Layer 10 (Background) → Background\n" +
            "  Layer 12 (Minimap Background) → MinimapBackground\n" +
            "  Layer 13 (OneWayPlatform) → OneWayPlatform\n" +
            "  Layer 3 (Ground) → Ground",
            MessageType.Info);

        EditorGUILayout.Space();
        prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);
        dryRun = EditorGUILayout.Toggle("Dry Run (Preview Only)", dryRun);

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix All Room Prefabs"))
        {
            FixAllRoomPrefabs();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Fix Tilemaps in Open Scene(s)"))
        {
            FixInOpenScenes();
        }
    }

    private string ResolveSortingLayer(GameObject go)
    {
        // Priority 1: Match by GameObject name
        if (nameToSortingLayer.TryGetValue(go.name, out string sortingByName))
            return sortingByName;

        // Priority 2: Match by Unity Layer
        if (layerToSortingLayer.TryGetValue(go.layer, out string sortingByLayer))
            return sortingByLayer;

        // No match — return null to skip
        return null;
    }

    private void FixAllRoomPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
        int totalFixed = 0;
        int totalSkipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            TilemapRenderer[] renderers = prefabRoot.GetComponentsInChildren<TilemapRenderer>(true);
            int fixedInPrefab = 0;

            foreach (TilemapRenderer tr in renderers)
            {
                string targetLayer = ResolveSortingLayer(tr.gameObject);

                if (targetLayer == null)
                {
                    totalSkipped++;
                    continue;
                }

                if (tr.sortingLayerName != targetLayer)
                {
                    if (dryRun)
                    {
                        Debug.Log($"[DRY RUN] {path} → \"{tr.gameObject.name}\" (Layer {LayerMask.LayerToName(tr.gameObject.layer)}): " +
                                  $"Sorting Layer \"{tr.sortingLayerName}\" → \"{targetLayer}\"");
                    }
                    else
                    {
                        tr.sortingLayerName = targetLayer;
                    }
                    fixedInPrefab++;
                    totalFixed++;
                }
            }

            if (!dryRun && fixedInPrefab > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                Debug.Log($"[SAVED] {path} — fixed {fixedInPrefab} TilemapRenderer(s).");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        if (dryRun)
        {
            Debug.Log($"[DRY RUN COMPLETE] Would fix {totalFixed} TilemapRenderers across {guids.Length} prefabs. " +
                      $"Skipped {totalSkipped} (no mapping). Uncheck 'Dry Run' to apply.");
        }
        else
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[DONE] Fixed {totalFixed} TilemapRenderers across {guids.Length} prefabs. Skipped {totalSkipped}.");
        }
    }

    private void FixInOpenScenes()
    {
        TilemapRenderer[] renderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        int count = 0;

        foreach (TilemapRenderer tr in renderers)
        {
            string targetLayer = ResolveSortingLayer(tr.gameObject);
            if (targetLayer == null) continue;

            if (tr.sortingLayerName != targetLayer)
            {
                if (dryRun)
                {
                    Debug.Log($"[DRY RUN] \"{tr.gameObject.name}\" (Layer {LayerMask.LayerToName(tr.gameObject.layer)}): " +
                              $"Sorting Layer \"{tr.sortingLayerName}\" → \"{targetLayer}\"");
                }
                else
                {
                    Undo.RecordObject(tr, "Fix Tilemap Sorting Layer");
                    tr.sortingLayerName = targetLayer;
                    EditorUtility.SetDirty(tr);
                }
                count++;
            }
        }

        string mode = dryRun ? "[DRY RUN]" : "[DONE]";
        Debug.Log($"{mode} {(dryRun ? "Would fix" : "Fixed")} {count} TilemapRenderers in open scene(s).");
    }
}
