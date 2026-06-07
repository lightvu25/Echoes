using UnityEngine;
using UnityEngine.Tilemaps;

public class MinimapTileFog : MonoBehaviour
{
    public Transform player;
    public Tilemap fogTilemap;
    public TileBase blackFogTile;

    [Header("Reveal Settings")]
    public int revealRadius = 4;

    private void Start()
    {
        StartCoroutine(SubscribeToMerger());
    }

    private System.Collections.IEnumerator SubscribeToMerger()
    {
        yield return new WaitUntil(() => RuntimeTilemapMerger.Instance != null);
        RuntimeTilemapMerger.Instance.OnMergeComplete += HandleMergeComplete;
    }

    private void OnDestroy()
    {
        if (RuntimeTilemapMerger.Instance != null)
            RuntimeTilemapMerger.Instance.OnMergeComplete -= HandleMergeComplete;
    }

    private void HandleMergeComplete()
    {
        if (RuntimeTilemapMerger.Instance != null && RuntimeTilemapMerger.Instance.globalGroundTilemap != null)
        {
            BoundsInt bounds = RuntimeTilemapMerger.Instance.globalGroundTilemap.cellBounds;
            FillWorldWithFog(bounds);
        }
    }

    public void FillWorldWithFog(BoundsInt worldBounds)
    {
        fogTilemap.ClearAllTiles();

        for (int x = worldBounds.xMin - 15; x < worldBounds.xMax + 15; x++)
        {
            for (int y = worldBounds.yMin - 15; y < worldBounds.yMax + 15; y++)
            {
                fogTilemap.SetTile(new Vector3Int(x, y, 0), blackFogTile);
            }
        }
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
            else return; 
        }

        if (fogTilemap == null) return;

        Vector3Int playerCell = fogTilemap.WorldToCell(player.position);

        for (int x = -revealRadius; x <= revealRadius; x++)
        {
            for (int y = -revealRadius; y <= revealRadius; y++)
            {
                if (x * x + y * y <= revealRadius * revealRadius)
                {
                    fogTilemap.SetTile(new Vector3Int(playerCell.x + x, playerCell.y + y, 0), null);
                }
            }
        }
    }
}