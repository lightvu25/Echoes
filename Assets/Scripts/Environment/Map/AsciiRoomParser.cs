using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ── Mapping structs ────────────────────────────────────────────────────── //

[Serializable]
public struct TileMapping
{
    public char character;
    public TileBase tile;
}

[Serializable]
public struct PrefabMapping
{
    public char character;
    public GameObject prefab;
    public Vector3 positionOffset;
}

public class AsciiRoomParser : MonoBehaviour
{
    [Header("Map Data")]
    public TextAsset mapBlueprint;

    [Header("Tilemaps")]
    public Tilemap groundTilemap;

    [Header("Mappings")]
    public List<TileMapping>   tileMappings   = new List<TileMapping>();
    public List<PrefabMapping> prefabMappings = new List<PrefabMapping>();

    [ContextMenu("Build Room from ASCII")]
    public void BuildRoom()
    {
        if (mapBlueprint == null) { Debug.LogError("[AsciiRoomParser] No mapBlueprint assigned.", this); return; }
        if (groundTilemap == null) { Debug.LogError("[AsciiRoomParser] No groundTilemap assigned.", this); return; }

        groundTilemap.ClearAllTiles();
        DestroySpawnedChildren();

        string[] lines = mapBlueprint.text.Split(
            new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int y = 0; y < lines.Length; y++)
        {
            string line = lines[y];

            for (int x = 0; x < line.Length; x++)
            {
                char c = line[x];

                // Text Y increases downward; Unity world Y increases upward.
                Vector3Int cellPos = new Vector3Int(x, -y, 0);

                if (TryGetTile(c, out TileBase tile))
                {
                    groundTilemap.SetTile(cellPos, tile);
                }
                else if (TryGetPrefab(c, out PrefabMapping pm))
                {
                    Vector3 worldPos = groundTilemap.GetCellCenterWorld(cellPos) + pm.positionOffset;
                    GameObject instance = Instantiate(pm.prefab, worldPos, Quaternion.identity, transform);
                    instance.name = $"{pm.prefab.name} [{x},{y}]";

                    if (instance.TryGetComponent(out Chest chest))
                    {
                        chest.persistenceID = $"Room_{gameObject.name}_Chest_X{cellPos.x}_Y{cellPos.y}";
                    }
                }
            }
        }

        Debug.Log($"[AsciiRoomParser] Built '{mapBlueprint.name}': {lines.Length} rows processed.", this);
    }

    private bool TryGetTile(char c, out TileBase tile)
    {
        foreach (TileMapping m in tileMappings)
        {
            if (m.character == c) { tile = m.tile; return tile != null; }
        }
        tile = null;
        return false;
    }

    private bool TryGetPrefab(char c, out PrefabMapping result)
    {
        foreach (PrefabMapping m in prefabMappings)
        {
            if (m.character == c && m.prefab != null) { result = m; return true; }
        }
        result = default;
        return false;
    }

    private void DestroySpawnedChildren()
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform child in transform)
            toDestroy.Add(child.gameObject);

#if UNITY_EDITOR
        foreach (var go in toDestroy)
            DestroyImmediate(go);
#else
        foreach (var go in toDestroy)
            Destroy(go);
#endif
    }
}
