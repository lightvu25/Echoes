using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class GiveAllItemsMenu
{
    [MenuItem("Tools/Echoes/Debug/Spawn All Tools and Relics")]
    public static void SpawnAllItems()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Debug] You must be in Play Mode to spawn items!");
            return;
        }

        if (PlayerInventoryCore.Instance == null)
        {
            Debug.LogError("[Debug] PlayerInventoryCore not found! Make sure the player is in the scene.");
            return;
        }

        GameObject dropPrefab = GetItemDropPrefab();
        if (dropPrefab == null)
        {
            Debug.LogError("[Debug] Could not find the ItemDrop prefab in the project.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemBaseData");
        int count = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemBaseData item = AssetDatabase.LoadAssetAtPath<ItemBaseData>(path);
            
            if (item != null && (item.Category == ItemCategory.Relic || item.Category == ItemCategory.Tool))
            {
                SpawnDrop(dropPrefab, item);
                count++;
            }
        }
        
        Debug.Log($"[Debug] Spawned {count} items around the player! Pick them up to equip.");
    }

    private static GameObject GetItemDropPrefab()
    {
        // Direct path lookup (foolproof)
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/Drop/Item Drop.prefab");
        if (prefab != null && prefab.GetComponent<ItemDrop>() != null)
        {
            return prefab;
        }

        // Fallback search
        string[] guids = AssetDatabase.FindAssets("Item Drop t:GameObject");
        foreach(string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<ItemDrop>() != null)
            {
                return prefab;
            }
        }
        return null;
    }

    private static void SpawnDrop(GameObject prefab, ItemBaseData item)
    {
        Vector3 playerPos = PlayerInventoryCore.Instance.transform.position;
        // Generate a random position in a circle around the player
        Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(2f, 4f);
        Vector3 spawnPos = playerPos + new Vector3(randomOffset.x, randomOffset.y + 1f, 0);

        GameObject dropObj = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        ItemDrop drop = dropObj.GetComponent<ItemDrop>();
        
        if (drop != null)
        {
            // Add a little pop effect
            Vector2 force = new Vector2(Random.Range(-2f, 2f), Random.Range(3f, 6f));
            drop.Initialize(force, item);
        }
    }
}
