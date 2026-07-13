using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class RoomOutlineGenerator
{
    [MenuItem("Window/Tools/Generate Outline From Minimap Background")]
    public static void GenerateOutlineForAllRooms()
    {
        // Find all prefabs in the Rooms folder
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Rooms" });
        int processedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Check if it actually has a Room component first without fully loading the contents
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null || prefabAsset.GetComponent<Room>() == null) continue;

            // Load prefab contents for editing
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            Room room = contents.GetComponent<Room>();
            
            bool modified = ProcessRoom(room);

            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                processedCount++;
            }
            
            PrefabUtility.UnloadPrefabContents(contents);
        }

        if (processedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[RoomOutlineGenerator] Successfully generated outlines for {processedCount} Room prefab(s).");
        }
        else
        {
            Debug.LogWarning("[RoomOutlineGenerator] No rooms were updated. Make sure 'Minimap Background' tilemaps exist and are not empty.");
        }
    }

    private static bool ProcessRoom(Room room)
    {
        // 1. Find game object with the "Minimap Background" layer under the Room
        int targetLayer = LayerMask.NameToLayer("Minimap Background");
        if (targetLayer == -1)
        {
            Debug.LogError("Layer 'Minimap Background' does not exist in the project!");
            return false;
        }

        Transform minimapBgTransform = FindChildByLayer(room.transform, targetLayer);

        if (minimapBgTransform == null)
        {
            Debug.LogWarning($"Could not find child with 'Minimap Background' layer in {room.name}");
            return false;
        }

        Tilemap tilemap = minimapBgTransform.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogWarning($"The object on 'Minimap Background' layer in {room.name} does not have a Tilemap component.");
            return false;
        }

        // Keep track of existing Rigidbody2D to avoid deleting it if it was already there
        Rigidbody2D existingRb = minimapBgTransform.GetComponent<Rigidbody2D>();

        // 2. Temporarily attach TilemapCollider2D and CompositeCollider2D
        TilemapCollider2D tilemapCollider = minimapBgTransform.gameObject.AddComponent<TilemapCollider2D>();
        
        // Use compositeOperation for newer Unity versions
#if UNITY_2023_1_OR_NEWER
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
        tilemapCollider.usedByComposite = true;
#endif
        
        CompositeCollider2D compositeCollider = minimapBgTransform.gameObject.AddComponent<CompositeCollider2D>();
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;

        Rigidbody2D currentRb = minimapBgTransform.GetComponent<Rigidbody2D>();
        if (currentRb != null)
        {
            currentRb.bodyType = RigidbodyType2D.Static;
        }

        // Force generate geometry in Editor
        compositeCollider.GenerateGeometry();

        // 3. Copy all the coordinates of that outline
        int pathCount = compositeCollider.pathCount;
        if (pathCount == 0)
        {
            Debug.LogWarning($"CompositeCollider2D generated 0 paths for {room.name}. Ensure the tilemap is not empty.");
            Cleanup(compositeCollider, tilemapCollider, existingRb == null ? currentRb : null);
            return false;
        }

        // 4. Paste the outline data into the PolygonCollider2D on the original Room object
        PolygonCollider2D roomPolygon = room.GetComponent<PolygonCollider2D>();
        if (roomPolygon == null)
        {
            roomPolygon = room.gameObject.AddComponent<PolygonCollider2D>();
        }

        roomPolygon.isTrigger = true;
        roomPolygon.pathCount = pathCount;
        for (int i = 0; i < pathCount; i++)
        {
            Vector2[] path = new Vector2[compositeCollider.GetPathPointCount(i)];
            compositeCollider.GetPath(i, path);

            // The path vertices are in the local space of the minimapBgTransform.
            // We need to convert them to the local space of the room object.
            for (int j = 0; j < path.Length; j++)
            {
                // Local (minimapBgTransform) -> World -> Local (room)
                Vector3 worldPt = minimapBgTransform.TransformPoint(path[j]);
                Vector3 localPt = room.transform.InverseTransformPoint(worldPt);
                path[j] = localPt;
            }

            roomPolygon.SetPath(i, path);
        }

        // 5. Remove temporary components
        Cleanup(compositeCollider, tilemapCollider, existingRb == null ? currentRb : null);

        return true; // We successfully modified the prefab
    }

    private static void Cleanup(CompositeCollider2D compositeCollider, TilemapCollider2D tilemapCollider, Rigidbody2D rbToRemove)
    {
        if (compositeCollider != null) Object.DestroyImmediate(compositeCollider);
        if (tilemapCollider != null) Object.DestroyImmediate(tilemapCollider);
        if (rbToRemove != null) Object.DestroyImmediate(rbToRemove);
    }

    private static Transform FindChildByLayer(Transform parent, int layerIndex)
    {
        // Breadth-first search
        foreach (Transform child in parent)
        {
            if (child.gameObject.layer == layerIndex)
                return child;
        }
        
        foreach (Transform child in parent)
        {
            Transform result = FindChildByLayer(child, layerIndex);
            if (result != null)
                return result;
        }

        return null;
    }
}
