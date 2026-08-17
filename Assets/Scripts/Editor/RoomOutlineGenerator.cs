using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

public class RoomOutlineGenerator
{
    [MenuItem("Tools/Echoes/Generate Outline From Minimap Background")]
    public static void GenerateOutlineForAllRooms()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            EditorUtility.DisplayDialog(
                "Close Prefab Mode First",
                "Close the currently opened room prefab before regenerating CameraBounds. " +
                "An open Prefab Stage can keep stale polygon data and overwrite the generated asset through Auto Save.",
                "OK");
            return;
        }

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

        // Reset both destination polygons before calculating fresh geometry. This
        // prevents old paths, offsets, and moved CameraBounds transforms from being
        // mixed with the newly generated outline.
        PolygonCollider2D roomPolygon = room.GetComponent<PolygonCollider2D>();
        if (roomPolygon == null)
            roomPolygon = room.gameObject.AddComponent<PolygonCollider2D>();
        roomPolygon.isTrigger = true;
        ResetPolygon(roomPolygon);

        Transform boundsTransform = room.transform.Find("CameraBounds");
        GameObject cameraBoundsObj;
        if (boundsTransform == null)
        {
            cameraBoundsObj = new GameObject("CameraBounds");
        }
        else
        {
            cameraBoundsObj = boundsTransform.gameObject;
        }

        Transform cameraBoundsTransform = cameraBoundsObj.transform;
        cameraBoundsTransform.SetParent(room.transform, false);
        cameraBoundsTransform.localPosition = Vector3.zero;
        cameraBoundsTransform.localRotation = Quaternion.identity;
        cameraBoundsTransform.localScale = Vector3.one;

        PolygonCollider2D boundsPolygon = cameraBoundsObj.GetComponent<PolygonCollider2D>();
        if (boundsPolygon == null)
            boundsPolygon = cameraBoundsObj.AddComponent<PolygonCollider2D>();
        boundsPolygon.isTrigger = true;
        ResetPolygon(boundsPolygon);
        room.CameraBoundsCollider = boundsPolygon;

        // Compress first so unused Tilemap cell bounds cannot inflate or displace
        // the temporary collider geometry.
        tilemap.CompressBounds();
        tilemap.RefreshAllTiles();

        Rigidbody2D existingBody = minimapBgTransform.GetComponent<Rigidbody2D>();
        Rigidbody2D body = existingBody != null
            ? existingBody
            : minimapBgTransform.gameObject.AddComponent<Rigidbody2D>();
        RigidbodyType2D originalBodyType = body.bodyType;

        TilemapCollider2D existingTilemapCollider = minimapBgTransform.GetComponent<TilemapCollider2D>();
        TilemapCollider2D tilemapCollider = existingTilemapCollider != null
            ? existingTilemapCollider
            : minimapBgTransform.gameObject.AddComponent<TilemapCollider2D>();
        Collider2D.CompositeOperation originalCompositeOperation = tilemapCollider.compositeOperation;

        CompositeCollider2D existingComposite = minimapBgTransform.GetComponent<CompositeCollider2D>();
        CompositeCollider2D compositeCollider = existingComposite != null
            ? existingComposite
            : minimapBgTransform.gameObject.AddComponent<CompositeCollider2D>();
        CompositeCollider2D.GeometryType originalGeometryType = compositeCollider.geometryType;
        float originalVertexDistance = compositeCollider.vertexDistance;

        try
        {
            body.bodyType = RigidbodyType2D.Static;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compositeCollider.vertexDistance = 0.05f;
            tilemapCollider.ProcessTilemapChanges();
            compositeCollider.GenerateGeometry();

            int pathCount = compositeCollider.pathCount;
            if (pathCount == 0)
            {
                Debug.LogWarning($"CompositeCollider2D generated 0 paths for {room.name}. Ensure the tilemap is not empty.");
                return false;
            }

            roomPolygon.pathCount = pathCount;
            boundsPolygon.pathCount = pathCount;

            for (int i = 0; i < pathCount; i++)
            {
                Vector2[] sourcePath = new Vector2[compositeCollider.GetPathPointCount(i)];
                Vector2[] roomPath = new Vector2[sourcePath.Length];
                Vector2[] boundsPath = new Vector2[sourcePath.Length];
                compositeCollider.GetPath(i, sourcePath);

                for (int j = 0; j < sourcePath.Length; j++)
                {
                    Vector3 worldPoint = minimapBgTransform.TransformPoint(sourcePath[j]);
                    roomPath[j] = room.transform.InverseTransformPoint(worldPoint);
                    boundsPath[j] = cameraBoundsTransform.InverseTransformPoint(worldPoint);
                }

                roomPolygon.SetPath(i, roomPath);
                boundsPolygon.SetPath(i, boundsPath);
            }

            return true;
        }
        finally
        {
            if (existingComposite == null)
                Object.DestroyImmediate(compositeCollider);
            else
            {
                compositeCollider.geometryType = originalGeometryType;
                compositeCollider.vertexDistance = originalVertexDistance;
            }

            if (existingTilemapCollider == null)
                Object.DestroyImmediate(tilemapCollider);
            else
                tilemapCollider.compositeOperation = originalCompositeOperation;

            if (existingBody == null)
                Object.DestroyImmediate(body);
            else
                body.bodyType = originalBodyType;
        }
    }

    private static void ResetPolygon(PolygonCollider2D polygon)
    {
        polygon.offset = Vector2.zero;
        polygon.pathCount = 0;
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
