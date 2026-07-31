using UnityEditor;
using UnityEngine;

public class BackgroundLayerFixer : EditorWindow
{
    [MenuItem("Tools/Echoes/Fix Background Sorting Order in Rooms")]
    public static void FixBackgroundSortingOrder()
    {
        string[] searchFolders = new string[] { "Assets/Prefabs/Rooms" };
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
        
        int backgroundLayerIndex = LayerMask.NameToLayer("Background");
        
        if (backgroundLayerIndex == -1)
        {
            Debug.LogError("Layer 'Background' does not exist in the project!");
            return;
        }

        int modifiedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // We need to load the prefab using AssetDatabase.LoadAssetAtPath
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                bool modified = false;
                
                // Get all Renderers in the prefab (TilemapRenderer, SpriteRenderer, etc)
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer sr in renderers)
                {
                    if (sr.gameObject.layer == backgroundLayerIndex || sr.gameObject.name == "Background")
                    {
                        if (sr.sortingOrder != -50)
                        {
                            sr.sortingOrder = -50;
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                    modifiedCount++;
                    Debug.Log($"Updated Backgrounds in: {path}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"Finished fixing background sorting orders. Modified {modifiedCount} prefabs.");
    }
}
