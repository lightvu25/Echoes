using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralRoomFactoryEditor : EditorWindow
{
    private GameObject _baseRoomPrefab;
    private int _numberOfVariations = 5;
    private string _targetFolder = "Assets/Prefabs/Rooms/Generated";

    // New Fields for Tilemap Generation
    private TileBase _baseRuleTile;
    private Vector2Int _roomWidthRange = new Vector2Int(15, 30);
    private Vector2Int _roomHeightRange = new Vector2Int(10, 20);
    private float _platformDensity = 0.5f;

    [MenuItem("Window/Tools/Room Factory")]
    public static void ShowWindow()
    {
        GetWindow<ProceduralRoomFactoryEditor>("Room Factory");
    }

    private void OnGUI()
    {
        GUILayout.Label("Procedural Room Factory", EditorStyles.boldLabel);

        _baseRoomPrefab = (GameObject)EditorGUILayout.ObjectField("Base Room Prefab", _baseRoomPrefab, typeof(GameObject), false);
        _baseRuleTile = (TileBase)EditorGUILayout.ObjectField("Base Rule Tile", _baseRuleTile, typeof(TileBase), false);
        
        _numberOfVariations = EditorGUILayout.IntField("Number of Variations", _numberOfVariations);
        _roomWidthRange = EditorGUILayout.Vector2IntField("Room Width Range", _roomWidthRange);
        _roomHeightRange = EditorGUILayout.Vector2IntField("Room Height Range", _roomHeightRange);
        _platformDensity = EditorGUILayout.Slider("Platform Density", _platformDensity, 0f, 1f);
        
        _targetFolder = EditorGUILayout.TextField("Target Folder", _targetFolder);

        if (GUILayout.Button("Generate"))
        {
            GenerateRooms();
        }
    }

    private void GenerateRooms()
    {
        if (_baseRoomPrefab == null)
        {
            Debug.LogError("Base Room Prefab is missing!");
            return;
        }

        EnsureFolderExists(_targetFolder);

        for (int i = 0; i < _numberOfVariations; i++)
        {
            GameObject tempInstance = Instantiate(_baseRoomPrefab);
            tempInstance.name = $"{_baseRoomPrefab.name}_Variant_{i + 1}";

            int currentWidth = Random.Range(_roomWidthRange.x, _roomWidthRange.y + 1);
            int currentHeight = Random.Range(_roomHeightRange.x, _roomHeightRange.y + 1);

            // 1. Tilemap Generation
            Tilemap tilemap = tempInstance.GetComponentInChildren<Tilemap>();
            if (tilemap != null && _baseRuleTile != null)
            {
                tilemap.ClearAllTiles();

                // Draw Floor and Ceiling
                for (int x = 0; x <= currentWidth; x++)
                {
                    tilemap.SetTile(new Vector3Int(x, 0, 0), _baseRuleTile);
                    tilemap.SetTile(new Vector3Int(x, currentHeight, 0), _baseRuleTile);
                }

                // Draw Walls
                for (int y = 0; y <= currentHeight; y++)
                {
                    // Leave a gap of 3 tiles high (from Y=1 to Y=3) at bottom corners for Exits
                    if (y >= 1 && y <= 3) continue;

                    tilemap.SetTile(new Vector3Int(0, y, 0), _baseRuleTile);
                    tilemap.SetTile(new Vector3Int(currentWidth, y, 0), _baseRuleTile);
                }

                // Draw Platforms
                // Start from Y=5 to leave space above exits, up to currentHeight-3
                for (int y = 5; y <= currentHeight - 3; y += 3)
                {
                    if (Random.value < _platformDensity)
                    {
                        int platformWidth = Random.Range(3, 6);
                        // Prevent platforms from touching walls
                        int startX = Random.Range(3, currentWidth - platformWidth - 2);

                        for (int px = startX; px < startX + platformWidth; px++)
                        {
                            tilemap.SetTile(new Vector3Int(px, y, 0), _baseRuleTile);
                        }
                    }
                }

                tilemap.CompressBounds();
            }
            else if (tilemap == null)
            {
                Debug.LogWarning($"No Tilemap found on {_baseRoomPrefab.name}. Tiles will not be generated.");
            }

            // 2. Adjust Exits, Spawners, and Props
            Transform[] children = tempInstance.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == tempInstance.transform) continue;

                string lowerName = child.name.ToLower();
                
                // Exits
                if (lowerName.Contains("exit"))
                {
                    if (lowerName.Contains("left"))
                    {
                        child.localPosition = new Vector3(0, 2, 0);
                        child.localRotation = Quaternion.Euler(0, 0, 180);
                    }
                    else if (lowerName.Contains("right"))
                    {
                        child.localPosition = new Vector3(currentWidth, 2, 0);
                        child.localRotation = Quaternion.Euler(0, 0, 0);
                    }
                    else if (lowerName.Contains("up") || lowerName.Contains("top"))
                    {
                        child.localPosition = new Vector3(currentWidth / 2f, currentHeight, 0);
                        child.localRotation = Quaternion.Euler(0, 0, 90);
                    }
                    else if (lowerName.Contains("down") || lowerName.Contains("bottom"))
                    {
                        child.localPosition = new Vector3(currentWidth / 2f, 0, 0);
                        child.localRotation = Quaternion.Euler(0, 0, -90);
                    }
                }
                // Spawners and Props
                else if (lowerName.Contains("spawner") || lowerName.Contains("prop"))
                {
                    Vector3 pos = child.localPosition;
                    // Clamp inside the tilemap boundaries (X between 2 and currentWidth-2)
                    pos.x = Mathf.Clamp(pos.x, 2, currentWidth - 2);
                    pos.y = Mathf.Clamp(pos.y, 2, currentHeight - 2);
                    
                    child.localPosition = pos;
                }
            }

            // 3. Update BoxCollider2D
            BoxCollider2D boxCollider = tempInstance.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = tempInstance.GetComponentInChildren<BoxCollider2D>();
            }

            if (boxCollider != null)
            {
                boxCollider.size = new Vector2(currentWidth, currentHeight);
                boxCollider.offset = new Vector2(currentWidth / 2f, currentHeight / 2f);
            }

            // Save Prefab
            string fullPath = $"{_targetFolder}/{tempInstance.name}.prefab";
            fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

            PrefabUtility.SaveAsPrefabAsset(tempInstance, fullPath);
            DestroyImmediate(tempInstance);
        }

        AssetDatabase.Refresh();
        Debug.Log($"Successfully generated {_numberOfVariations} upgraded room variants in {_targetFolder}");
    }

    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] folders = folderPath.Split('/');
        string currentPath = "";
        
        for (int i = 0; i < folders.Length; i++)
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                currentPath = folders[i];
                continue;
            }

            string newPath = $"{currentPath}/{folders[i]}";
            if (!AssetDatabase.IsValidFolder(newPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }
            currentPath = newPath;
        }
    }
}
