using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ModifierNodeUIController : MonoBehaviour, IUIPanel
{
    [System.Serializable]
    public class ModifierSlot
    {
        public string nodeName; // e.g. "Relic", "Echo", "Equipment"
        public Image iconImage;
        public Image backgroundImage;
        public TextMeshProUGUI statusText; // Connected / Cut
        
        [TextArea(3, 5)]
        [Tooltip("The stats/penalties to preview when this slot is highlighted.")]
        public string previewData;
    }

    [Header("UI Slots Configuration")]
    [Tooltip("Index 0: Relic, Index 1: Echo, Index 2: Equipment")]
    public ModifierSlot[] slots = new ModifierSlot[3];

    [Header("Preview Panel")]
    public TextMeshProUGUI previewSideText;

    [Header("Visual Settings")]
    public Color normalBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color highlightedBackgroundColor = new Color(0.4f, 0.4f, 0.15f, 1f);
    
    public Color connectedTextColor = Color.green;
    public Color cutTextColor = Color.red;

    [Header("Events")]
    public UnityEvent<int, bool> OnNodeToggled;

    private int _selectedIndex = 0;
    private bool[] _connectedStates = new bool[3];
    private bool _isAcceptingInput = false;

    private void Awake()
    {
        // Initialize all slots as connected by default (or the backend can set this later)
        for (int i = 0; i < 3; i++)
        {
            _connectedStates[i] = true;
        }
    }

    private void Start()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _isAcceptingInput = true;
        _selectedIndex = 0; // Reset to top when opened
        UpdateSelectionVisuals();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _isAcceptingInput = false;
    }

    private void Update()
    {
        if (!_isAcceptingInput) return;

        // K moves UP (decrement index)
        if (Input.GetKeyDown(KeyCode.K))
        {
            _selectedIndex--;
            if (_selectedIndex < 0) _selectedIndex = slots.Length - 1;
            UpdateSelectionVisuals();
        }
        // J moves DOWN (increment index)
        else if (Input.GetKeyDown(KeyCode.J))
        {
            _selectedIndex++;
            if (_selectedIndex >= slots.Length) _selectedIndex = 0;
            UpdateSelectionVisuals();
        }

        // Toggle state
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleCurrentNode();
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            bool isConnected = _connectedStates[i];

            // 1. Highlight the currently selected slot
            if (slot.backgroundImage != null)
            {
                slot.backgroundImage.color = (i == _selectedIndex) ? highlightedBackgroundColor : normalBackgroundColor;
            }

            // 2. Visual state of the Connection (Green check/text vs Red strike)
            if (slot.statusText != null)
            {
                if (isConnected)
                {
                    slot.statusText.text = "CONNECTED";
                    slot.statusText.color = connectedTextColor;
                    slot.statusText.fontStyle = FontStyles.Normal;
                }
                else
                {
                    slot.statusText.text = "CUT";
                    slot.statusText.color = cutTextColor;
                    slot.statusText.fontStyle = FontStyles.Strikethrough;
                }
            }
        }

        // 3. Dynamically update the side-panel stats for the highlighted node
        if (previewSideText != null && _selectedIndex >= 0 && _selectedIndex < slots.Length)
        {
            previewSideText.text = slots[_selectedIndex].previewData;
        }
    }

    public void ToggleCurrentNode()
    {
        // Toggle the boolean state
        _connectedStates[_selectedIndex] = !_connectedStates[_selectedIndex];
        
        // Refresh the UI to immediately show the new state
        UpdateSelectionVisuals();

        // Expose to backend logic
        OnNodeToggled?.Invoke(_selectedIndex, _connectedStates[_selectedIndex]);
    }
    
    /// <summary>
    /// Utility method for the backend to sync the UI with existing saved states.
    /// </summary>
    public void SetNodeState(int index, bool isConnected)
    {
        if (index >= 0 && index < _connectedStates.Length)
        {
            _connectedStates[index] = isConnected;
            if (gameObject.activeInHierarchy)
            {
                UpdateSelectionVisuals();
            }
        }
    }
}
