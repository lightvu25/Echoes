using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBoundsCompressor
{
    [MenuItem("Tools/Echoes/Compress All Room Tilemaps")]
    public static void CompressAllRoomTilemaps()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Rooms" });
        int prefabCount = 0;
        int tilemapCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Tilemap[] tilemaps = prefab.GetComponentsInChildren<Tilemap>(true);
            if (tilemaps.Length > 0)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                Tilemap[] instanceTilemaps = contents.GetComponentsInChildren<Tilemap>(true);
                
                bool modified = false;
                foreach (Tilemap tm in instanceTilemaps)
                {
                    tm.CompressBounds();
                    tilemapCount++;
                    modified = true;
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    prefabCount++;
                }
                
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        Debug.Log($"Successfully compressed {tilemapCount} tilemaps across {prefabCount} room prefabs.");
    }
}
