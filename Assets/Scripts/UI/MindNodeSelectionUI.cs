using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class MindNodeSelectionUI : MonoBehaviour, IUIPanel
{
    [Header("UI References")]
    public TextMeshProUGUI[] optionTexts;
    public Image[] optionIcons; // Add this array for the icons!
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    
    [Header("Icon Mappings")]
    public Sprite echoIcon;
    public Sprite relicIcon;
    public Sprite equipIcon;
    public Sprite normalIcon;
    public Sprite mindGardenIcon;
    public Sprite exitIcon;
    
    private List<MindNodeConnection> _availableConnections = new List<MindNodeConnection>();
    private int _selectedIndex = 0;
    private MindNode _sourceNode;
    private bool _isAcceptingInput = false;

    private void Start()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _isAcceptingInput = true;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        _isAcceptingInput = false;
    }

    public void SetupSelection(MindNode sourceNode, List<MindNodeConnection> connections)
    {
        _sourceNode = sourceNode;
        _availableConnections = connections.Where(c => c != null && c.targetNode != null).ToList();

        if (_availableConnections.Count == 0)
        {
            Debug.LogWarning("[MindNodeSelectionUI] No valid connections available!");
            Hide();
            return;
        }

        _selectedIndex = 0;

        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (i < _availableConnections.Count)
            {
                optionTexts[i].gameObject.SetActive(true);
                string dirName = GetDirectionName(_sourceNode, _availableConnections[i]);
                string status = _availableConnections[i].isCut ? "<color=red>[CUT]</color>" : "<color=green>[CONN]</color>";
                optionTexts[i].text = $"{status} {dirName} Path - {_availableConnections[i].targetNode.nodeType}";

                if (optionIcons != null && i < optionIcons.Length && optionIcons[i] != null)
                {
                    optionIcons[i].gameObject.SetActive(true);
                    optionIcons[i].sprite = GetIconForNode(_availableConnections[i].targetNode.nodeType);
                }
            }
            else
            {
                optionTexts[i].gameObject.SetActive(false);
                
                if (optionIcons != null && i < optionIcons.Length && optionIcons[i] != null)
                {
                    optionIcons[i].gameObject.SetActive(false);
                }
            }
        }

        UpdateVisuals();
        Show();
    }

    private Sprite GetIconForNode(NodeType type)
    {
        switch (type)
        {
            case NodeType.Echo: return echoIcon;
            case NodeType.Relic: return relicIcon;
            case NodeType.Equipment: return equipIcon;
            case NodeType.MindGarden: return mindGardenIcon;
            case NodeType.MapExit: return exitIcon;
            default: return normalIcon;
        }
    }

    private string GetDirectionName(MindNode source, MindNodeConnection connection)
    {
        if (source.upConnection == connection) return "Up";
        if (source.downConnection == connection) return "Down";
        if (source.leftConnection == connection) return "Left";
        if (source.rightConnection == connection) return "Right";
        return "Unknown";
    }

    private void Update()
    {
        if (!_isAcceptingInput) return;
        if (_availableConnections.Count == 0) return;

        bool upPressed = GameInput.Instance.IsUpActionPressed();
        bool downPressed = GameInput.Instance.IsDownActionPressed();
        if (Input.GetKeyDown(KeyCode.K))
        {
            _selectedIndex--;
            if (_selectedIndex < 0) _selectedIndex = _availableConnections.Count - 1;
            UpdateVisuals();
        }
        else if (Input.GetKeyDown(KeyCode.J))
        {
            _selectedIndex++;
            if (_selectedIndex >= _availableConnections.Count) _selectedIndex = 0;
            UpdateVisuals();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleCut();
        }
    }

    private void UpdateVisuals()
    {
        if (optionTexts == null || optionTexts.Length == 0)
        {
            Debug.LogWarning("[MindNodeSelectionUI] optionTexts array is empty! Please assign the Text elements in the Inspector.");
            return;
        }

        for (int i = 0; i < _availableConnections.Count; i++)
        {
            if (i >= optionTexts.Length) break;

            string dirName = GetDirectionName(_sourceNode, _availableConnections[i]);
            string status = _availableConnections[i].isCut ? "<color=red>[CUT]</color>" : "<color=green>[CONN]</color>";
            optionTexts[i].text = $"{status} {dirName} Path - {_availableConnections[i].targetNode.nodeType}";

            if (i == _selectedIndex)
            {
                optionTexts[i].color = highlightColor;
                optionTexts[i].fontStyle = FontStyles.Bold;
                if (optionIcons != null && i < optionIcons.Length && optionIcons[i] != null) optionIcons[i].color = highlightColor;
            }
            else
            {
                optionTexts[i].color = normalColor;
                optionTexts[i].fontStyle = FontStyles.Normal;
                if (optionIcons != null && i < optionIcons.Length && optionIcons[i] != null) optionIcons[i].color = normalColor;
            }
        }

        if (UIManager.Instance != null)
        {
            MindNodeUI previewUI = UIManager.Instance.GetPanel<MindNodeUI>(UIPanelType.MindNode);
            if (previewUI != null)
            {
                var targetNode = _availableConnections[_selectedIndex].targetNode;
                previewUI.DisplayNode(targetNode, GetIconForNode(targetNode.nodeType));
            }
        }
    }
    
    private void ToggleCut()
    {
        var selectedConn = _availableConnections[_selectedIndex];
        
        // Toggle the cut state
        selectedConn.isCut = !selectedConn.isCut;

        // Also toggle the reverse connection to prevent visual bugs in MindWorldRenderer
        if (selectedConn.targetNode != null)
        {
            MindNode target = selectedConn.targetNode;
            if (target.upConnection != null && target.upConnection.targetNode == _sourceNode) target.upConnection.isCut = selectedConn.isCut;
            if (target.downConnection != null && target.downConnection.targetNode == _sourceNode) target.downConnection.isCut = selectedConn.isCut;
            if (target.leftConnection != null && target.leftConnection.targetNode == _sourceNode) target.leftConnection.isCut = selectedConn.isCut;
            if (target.rightConnection != null && target.rightConnection.targetNode == _sourceNode) target.rightConnection.isCut = selectedConn.isCut;
        }

        // Update the visual lines in the world
        MindWorldRenderer worldRenderer = FindAnyObjectByType<MindWorldRenderer>();
        if (worldRenderer != null)
        {
            worldRenderer.RefreshAllPaths();
        }

        UpdateVisuals();
    }
}
