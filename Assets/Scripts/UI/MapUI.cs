using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public enum MapUIState
{
    ViewOnly,
    TeleportSelect
}
[RequireComponent(typeof(CanvasGroup))]
public class MapUI : MonoBehaviour, IUIPanel, IDragHandler
{
    public static MapUI Instance { get; private set; }

    public bool IsOpen { get; private set; }

    [Header("UI Graph Setup")]
    [Tooltip("The Content RectTransform of your Map Scroll View.")]
    [SerializeField] private RectTransform contentContainer;
    
    [Tooltip("The UI Button prefab spawned for each teleport node.")]
    [SerializeField] private GameObject teleportUIButtonPrefab;

    [Tooltip("The UI Image prefab spawned for each physical room (Fog of War).")]
    [SerializeField] private GameObject roomUIPrefab;

    [Tooltip("The UI Image prefab used to draw a line between teleporter nodes.")]
    [SerializeField] private GameObject lineUIPrefab;
    
    [Tooltip("The UI prefab representing the player's current position.")]
    [SerializeField] private GameObject playerIconPrefab;
    
    [Tooltip("Multiplier to convert world coordinates to Canvas anchored positions.")]
    [SerializeField] private float mapScale = 20f;

    [Header("Fog Of War Colors")]
    [SerializeField] private Color hiddenRoomColor = new Color(0f, 0f, 0f, 0f);
    [SerializeField] private Color exploredRoomColor = new Color(0.2f, 0.5f, 0.8f, 1f);

    [Header("Fog of War (Data-Driven)")]
    [SerializeField] private RectTransform fowRawImageRect;
    private Texture2D _fogTexture;

    [Header("UI Interactions")]
    public GameObject blackScreenBackground; 
    public UIDragHandler dragHandler; 
    public RectTransform playerIconRect; 

    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    
    // UI mapping state
    private Dictionary<Room, Graphic> _roomUIDict = new Dictionary<Room, Graphic>();
    private Dictionary<TeleporterNode, GameObject> _teleporterUIDict = new Dictionary<TeleporterNode, GameObject>();
    
    // Interaction state
    private MapUIState _currentState = MapUIState.ViewOnly;
    private TeleporterNode _selectedTargetNode = null;
    private RectTransform _lineInstance;
    private PlayerMovement _cachedPlayer;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();

        Room.OnRoomExplored += RevealRoom;
    }

    private void Start()
    {
        _canvasGroup.alpha = 0f;
        
        if (blackScreenBackground != null) blackScreenBackground.SetActive(false);
        if (dragHandler != null) dragHandler.enabled = false;


        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
    }

    private void OnEnable()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnCancelPressed += HandleCancelPressed;
            GameInput.Instance.OnMapTogglePressed += HandleMapToggle;
        }
    }

    private void OnDisable()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnCancelPressed -= HandleCancelPressed;
            GameInput.Instance.OnMapTogglePressed -= HandleMapToggle;
        }
    }

    private void OnDestroy()
    {
        Room.OnRoomExplored -= RevealRoom;
        if (_fogTexture != null) Destroy(_fogTexture);
    }

    private void HandleMapToggle()
    {
        if (IsOpen)
        {
            Hide();
            if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen)
            {
                UIManager.Instance.CloseCurrentPanel();
            }
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel(UIPanelType.Map);
            }
        }
    }

    private void HandleCancelPressed()
    {
        if (IsOpen && UIManager.Instance != null)
        {
            UIManager.Instance.CloseCurrentPanel();
        }
    }

    public void Show()
    {
        IsOpen = true;
        
        _canvasGroup.alpha = 1f; 
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        
        if (blackScreenBackground != null) blackScreenBackground.SetActive(true);
        if (dragHandler != null) dragHandler.enabled = true;

        if (TeleportManager.Instance != null && TeleportManager.Instance.CurrentActiveNode != null)
        {
            _currentState = MapUIState.TeleportSelect;
        }
        else
        {
            _currentState = MapUIState.ViewOnly;
        }

        _selectedTargetNode = null;
        if (_lineInstance != null) _lineInstance.gameObject.SetActive(false);

        // Dynamically instantiate only NEW teleporters
        SyncTeleporterIcons();

        // Generate data-driven fog
        GenerateFogTextureFromTilemap();
    }

    public void Hide()
    {
        IsOpen = false;
        
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        
        if (blackScreenBackground != null) blackScreenBackground.SetActive(false);
        if (dragHandler != null) dragHandler.enabled = false;

        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.CurrentActiveNode = null;
        }
        _selectedTargetNode = null;
        if (_lineInstance != null) _lineInstance.gameObject.SetActive(false);
    }

    public void InitializeMapGraph(IReadOnlyList<Room> rooms)
    {
        ClearMapGraph();

        if (contentContainer == null || rooms == null || rooms.Count == 0) return;

        if (roomUIPrefab != null)
        {
            foreach (Room room in rooms)
            {
                if (room == null) continue;
                Bounds bounds = room.OriginalBounds;

                GameObject roomUIObj = Instantiate(roomUIPrefab, contentContainer);
                Graphic roomGraphic = roomUIObj.GetComponent<Image>();
                RectTransform rect = roomUIObj.GetComponent<RectTransform>();

                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(bounds.center.x * mapScale, bounds.center.y * mapScale);
                    rect.sizeDelta = new Vector2(bounds.size.x * mapScale, bounds.size.y * mapScale);
                }

                PolygonCollider2D polyCol = room.GetComponent<PolygonCollider2D>();
                if (polyCol != null)
                {
                    DestroyImmediate(roomUIObj.GetComponent<Image>());
                    MapPolygonUI polyUI = roomUIObj.AddComponent<MapPolygonUI>();
                    polyUI.raycastTarget = false;
                    
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = new Vector2(room.transform.position.x * mapScale, room.transform.position.y * mapScale);
                    
                    polyUI.Initialize(polyCol, mapScale);
                    roomGraphic = polyUI;
                }

                if (roomGraphic != null)
                {
                    roomGraphic.color = room.isExplored ? exploredRoomColor : hiddenRoomColor;
                    _roomUIDict[room] = roomGraphic;
                }
            }
        }

        if (lineUIPrefab != null && _lineInstance == null)
        {
            GameObject lineObj = Instantiate(lineUIPrefab, contentContainer);
            _lineInstance = lineObj.GetComponent<RectTransform>();
            _lineInstance.gameObject.SetActive(false);
            _lineInstance.pivot = new Vector2(0f, 0.5f);
        }

        if (playerIconRect == null && playerIconPrefab != null)
        {
            GameObject pIconObj = Instantiate(playerIconPrefab, contentContainer);
            playerIconRect = pIconObj.GetComponent<RectTransform>();
            playerIconRect.localScale = Vector3.one;
            playerIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            playerIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            playerIconRect.pivot = new Vector2(0.5f, 0.5f);
            playerIconRect.gameObject.SetActive(true);
        }

        if (playerIconRect != null)
        {
            // Player icon goes under the fog, but over rooms and teleporters
            playerIconRect.SetAsLastSibling();
        }

        // The fog MUST be the absolute last sibling to cover everything
        if (fowRawImageRect != null)
        {
            fowRawImageRect.SetAsLastSibling();
        }
    }

    private void ClearMapGraph()
    {
        foreach (var kvp in _roomUIDict)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        _roomUIDict.Clear();

        foreach (var kvp in _teleporterUIDict)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        _teleporterUIDict.Clear();

        if (_lineInstance != null) Destroy(_lineInstance.gameObject);
        _lineInstance = null;
    }

    private void SyncTeleporterIcons()
    {
        if (TeleportManager.Instance == null || teleportUIButtonPrefab == null) return;

        foreach (TeleporterNode node in TeleportManager.Instance.unlockedNodes)
        {
            if (node == null || _teleporterUIDict.ContainsKey(node)) continue;

            GameObject iconObj = Instantiate(teleportUIButtonPrefab, contentContainer);
            _teleporterUIDict[node] = iconObj;

            RectTransform rect = iconObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Put teleporters on top of rooms (but under fog)
                iconObj.transform.SetAsLastSibling();
                rect.anchoredPosition = new Vector2(node.transform.position.x * mapScale, node.transform.position.y * mapScale);
            }

            Button btn = iconObj.GetComponent<Button>();
            if (btn != null)
            {
                TeleporterNode capturedNode = node; 
                btn.onClick.AddListener(() => OnNodeClicked(capturedNode));
            }
        }
    }

    private void OnNodeClicked(TeleporterNode targetNode)
    {
        if (_currentState == MapUIState.ViewOnly)
        {
            // Just viewing, clicking does nothing
            return;
        }

        if (_currentState == MapUIState.TeleportSelect)
        {
            if (_selectedTargetNode == targetNode)
            {
                // Double click confirmed!
                if (_cachedPlayer == null) _cachedPlayer = FindFirstObjectByType<PlayerMovement>();
                
                if (_cachedPlayer != null)
                {
                    TeleportManager.Instance.TeleportPlayerTo(targetNode, _cachedPlayer.transform.root);
                }
                else
                {
                    Debug.LogWarning("[MapUI] Player not found for teleportation!");
                }
                
                if (UIManager.Instance != null) UIManager.Instance.CloseCurrentPanel();
            }
            else
            {
                // First click, select and draw line
                _selectedTargetNode = targetNode;
                DrawLineBetween(TeleportManager.Instance.CurrentActiveNode, _selectedTargetNode);
            }
        }
    }

    private void DrawLineBetween(TeleporterNode startNode, TeleporterNode targetNode)
    {
        if (_lineInstance == null) return;

        if (startNode == null || targetNode == null)
        {
            _lineInstance.gameObject.SetActive(false);
            return;
        }

        if (!_teleporterUIDict.TryGetValue(startNode, out GameObject startUI) || 
            !_teleporterUIDict.TryGetValue(targetNode, out GameObject targetUI))
        {
            return;
        }

        _lineInstance.gameObject.SetActive(true);
        _lineInstance.SetAsLastSibling(); // Ensure it renders on top

        RectTransform startRect = startUI.GetComponent<RectTransform>();
        RectTransform targetRect = targetUI.GetComponent<RectTransform>();

        Vector2 startPos = startRect.anchoredPosition;
        Vector2 targetPos = targetRect.anchoredPosition;

        _lineInstance.anchoredPosition = startPos;
        
        Vector2 dir = targetPos - startPos;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        _lineInstance.sizeDelta = new Vector2(distance, _lineInstance.sizeDelta.y);
        _lineInstance.localEulerAngles = new Vector3(0, 0, angle);
    }

    public void RevealRoom(Room currentRoom)
    {
        if (currentRoom == null) return;
        currentRoom.isExplored = true;

        if (_roomUIDict.TryGetValue(currentRoom, out Graphic roomGraphic))
        {
            if (roomGraphic != null)
            {
                roomGraphic.color = exploredRoomColor;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsOpen || contentContainer == null) return;
        
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        contentContainer.anchoredPosition += eventData.delta / scaleFactor;
    }

    private void Update()
    {
        // Continuously track the player
        if (playerIconRect != null)
        {
            if (_cachedPlayer == null) _cachedPlayer = FindFirstObjectByType<PlayerMovement>();

            if (_cachedPlayer != null)
            {
                playerIconRect.gameObject.SetActive(true);
                playerIconRect.anchoredPosition = new Vector2(
                    _cachedPlayer.transform.position.x * mapScale, 
                    _cachedPlayer.transform.position.y * mapScale
                );
            }
            else
            {
                playerIconRect.gameObject.SetActive(false);
            }
        }
    }

    private void GenerateFogTextureFromTilemap()
    {
        MinimapTileFog tileFog = FindFirstObjectByType<MinimapTileFog>();
        if (tileFog == null || tileFog.fogTilemap == null) return;

        Tilemap fogTilemap = tileFog.fogTilemap;
        fogTilemap.CompressBounds();
        BoundsInt cellBounds = fogTilemap.cellBounds;

        if (cellBounds.size.x <= 0 || cellBounds.size.y <= 0) return;

        if (_fogTexture != null)
        {
            Destroy(_fogTexture);
        }

        _fogTexture = new Texture2D(cellBounds.size.x, cellBounds.size.y, TextureFormat.RGBA32, false);
        _fogTexture.filterMode = FilterMode.Point;

        Color darkFogColor = new Color(0, 0, 0, 1f);

        for (int y = 0; y < cellBounds.size.y; y++)
        {
            for (int x = 0; x < cellBounds.size.x; x++)
            {
                Vector3Int pos = new Vector3Int(cellBounds.xMin + x, cellBounds.yMin + y, 0);
                if (fogTilemap.HasTile(pos))
                {
                    _fogTexture.SetPixel(x, y, darkFogColor);
                }
                else
                {
                    _fogTexture.SetPixel(x, y, Color.clear);
                }
            }
        }

        _fogTexture.Apply();

        if (fowRawImageRect != null)
        {
            RawImage rawImg = fowRawImageRect.GetComponent<RawImage>();
            if (rawImg != null)
            {
                rawImg.texture = _fogTexture;
                rawImg.raycastTarget = false;
            }

            // CRITICAL FIX: Use the TilemapRenderer's WORLD space bounds to align perfectly with the UI Rooms,
            // which are also positioned using their WORLD space bounds!
            Bounds fogWorldBounds = fogTilemap.GetComponent<TilemapRenderer>().bounds;

            fowRawImageRect.sizeDelta = new Vector2(fogWorldBounds.size.x * mapScale, fogWorldBounds.size.y * mapScale);
            fowRawImageRect.anchoredPosition = new Vector2(fogWorldBounds.center.x * mapScale, fogWorldBounds.center.y * mapScale);
            fowRawImageRect.SetAsLastSibling();
        }
    }
}
