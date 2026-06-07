using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AsciiLevelGenerator : BaseLevelGenerator
{
    [Header("ASCII Map Data")]
    public TextAsset[] roomFiles; 

    [Header("Tilemaps & Tiles")]
    public Tilemap hitboxGroundTilemap;
    public TileBase groundHitboxTile;

    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject waterZonePrefab;

    private Transform playerSpawnTransform;

    public override void GenerateMap(int levelNumber)
    {
        hitboxGroundTilemap.ClearAllTiles();

        if (roomFiles == null || roomFiles.Length == 0)
        {
            Debug.LogError("Chưa có file ASCII nào được gán vào AsciiLevelGenerator!");
            return;
        }

        int roomIndex = Random.Range(0, roomFiles.Length);
        string mapText = roomFiles[roomIndex].text;

        string[] lines = mapText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        int mapHeight = lines.Length;

        for (int y = 0; y < mapHeight; y++)
        {
            string currentLine = lines[y];
            int mapWidth = currentLine.Length;

            int unityY = mapHeight - 1 - y;

            for (int x = 0; x < mapWidth; x++)
            {
                char c = currentLine[x];
                Vector3Int cellPosition = new Vector3Int(x, unityY, 0);
                
                Vector3 worldPos = hitboxGroundTilemap.GetCellCenterWorld(cellPosition);

                switch (c)
                {
                    case '#':
                        hitboxGroundTilemap.SetTile(cellPosition, groundHitboxTile);
                        break;

                    case 'E':
                        if (enemyPrefab != null)
                            Instantiate(enemyPrefab, worldPos, Quaternion.identity);
                        break;

                    case '~':
                        if (waterZonePrefab != null)
                            Instantiate(waterZonePrefab, worldPos, Quaternion.identity);
                        break;

                    case 'P':
                        if (playerSpawnTransform == null)
                        {
                            GameObject spawnObj = new GameObject("PlayerSpawnPoint");
                            playerSpawnTransform = spawnObj.transform;
                        }
                        playerSpawnTransform.position = worldPos;
                        break;

                    case '.': 
                    default:
                        break;
                }
            }
        }
    }

    public override Transform GetPlayerSpawnPoint()
    {
        if (playerSpawnTransform == null)
        {
            Debug.LogWarning("Không tìm thấy ký tự 'P' trong file ASCII. Đẻ Player tại (0,0,0).");
            GameObject fallbackSpawn = new GameObject("FallbackPlayerSpawn");
            fallbackSpawn.transform.position = Vector3.zero;
            return fallbackSpawn.transform;
        }
        return playerSpawnTransform;
    }
}