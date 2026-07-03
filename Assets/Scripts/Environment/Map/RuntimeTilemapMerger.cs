using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RuntimeTilemapMerger : MonoBehaviour
{
    public static RuntimeTilemapMerger Instance { get; private set; }

    public event System.Action OnMergeComplete;
    public bool IsMerging { get; private set; }

    [Header("Global Tilemaps")]
    public Tilemap globalGroundTilemap;
    public Tilemap globalBackgroundTilemap;

    [Header("Auto-Fill Gaps")]
    public RuleTile fillGroundRuleTile;
    public int fillPadding = 15;

    private void Awake()
    {
        Instance = this;
    }

    public void MergeAllRooms(List<GameObject> spawnedRooms)
    {
        IsMerging = true;
        Physics2D.simulationMode = SimulationMode2D.Script;
        StartCoroutine(MergeRoutine(spawnedRooms));
    }

    private IEnumerator MergeRoutine(List<GameObject> spawnedRooms)
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        int bgLayer = LayerMask.NameToLayer("Background");
        int minimapLayer = LayerMask.NameToLayer("Minimap Background");

        HashSet<Vector3Int> globalRoomFootprint = new HashSet<Vector3Int>();

        foreach (GameObject roomObj in spawnedRooms)
        {
            if (roomObj == null) continue;

            Tilemap[] localTilemaps = roomObj.GetComponentsInChildren<Tilemap>();

            foreach (Tilemap localTm in localTilemaps)
            {
                bool isGround = localTm.gameObject.layer == groundLayer;
                bool isBg = localTm.gameObject.layer == bgLayer;
                bool isMinimap = localTm.gameObject.layer == minimapLayer || localTm.gameObject.name.Contains("Minimap");

                if (!isGround && !isBg && !isMinimap) continue;

                BoundsInt bounds = localTm.cellBounds;

                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        Vector3Int localCellPos = new Vector3Int(x, y, 0);
                        TileBase tile = localTm.GetTile(localCellPos);

                        if (tile != null)
                        {
                            Vector3 worldPos = localTm.GetCellCenterWorld(localCellPos);
                            Vector3Int globalCellPos = globalGroundTilemap.WorldToCell(worldPos);
                            globalCellPos.z = 0;

                            if (isGround && globalGroundTilemap != null)
                                globalGroundTilemap.SetTile(globalCellPos, tile);

                            if (isBg && globalBackgroundTilemap != null)
                                globalBackgroundTilemap.SetTile(globalCellPos, tile);

                            globalRoomFootprint.Add(globalCellPos);
                        }
                    }
                }
                
                if (isGround || isBg) localTm.gameObject.SetActive(false);
            }
            yield return null;
        }

        if (fillGroundRuleTile != null && globalGroundTilemap != null)
        {
            BoundsInt globalBounds = globalGroundTilemap.cellBounds;

            int startX = globalBounds.xMin - fillPadding;
            int endX = globalBounds.xMax + fillPadding;
            int startY = globalBounds.yMin - fillPadding;
            int endY = globalBounds.yMax + fillPadding;

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    if (!globalRoomFootprint.Contains(pos) && globalGroundTilemap.GetTile(pos) == null)
                    {
                        globalGroundTilemap.SetTile(pos, fillGroundRuleTile);
                    }
                }
            }
        }

        if (globalGroundTilemap != null) 
        {
            globalGroundTilemap.RefreshAllTiles();
            CompositeCollider2D comp = globalGroundTilemap.GetComponent<CompositeCollider2D>();
            if (comp != null) comp.GenerateGeometry();
        }
        if (globalBackgroundTilemap != null) globalBackgroundTilemap.RefreshAllTiles();

        IsMerging = false;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        OnMergeComplete?.Invoke();
    }
}