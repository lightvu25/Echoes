using UnityEditor;
using UnityEngine;

public class ColliderGeometryConverter
{
    [MenuItem("Window/Tools/Convert Colliders to Polygons")]
    public static void ConvertColliders()
    {
        // Find all prefabs in the specified directory
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Rooms" });
        int updatedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Load prefab contents safely (Modern Unity Prefab Workflow)
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null) continue;

            // Find all CompositeCollider2D components on this prefab (including children)
            CompositeCollider2D[] colliders = contents.GetComponentsInChildren<CompositeCollider2D>(true);
            bool modified = false;

            foreach (CompositeCollider2D col in colliders)
            {
                // Check if geometryType needs to be changed
                if (col.geometryType != CompositeCollider2D.GeometryType.Polygons)
                {
                    col.geometryType = CompositeCollider2D.GeometryType.Polygons;
                    modified = true;
                }
            }

            // Save and clean up if modifications were made
            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                updatedCount++;
            }
            
            // Unload to free memory and complete the workflow
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // Log the clean summary
        Debug.Log($"Successfully converted CompositeCollider2D geometry to Polygons on {updatedCount} room prefabs.");
    }
}
