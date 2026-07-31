using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class LevelBlueprintEditorWindow : EditorWindow
{
    private LevelBlueprintGraphView _graphView;
    private LevelBlueprint _currentBlueprint;
    private ObjectField _blueprintField;

    [MenuItem("Tools/Echoes/Map Generator/Blueprint Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<LevelBlueprintEditorWindow>("Blueprint Editor");
        window.Show();
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
    }

    private void OnDisable()
    {
        if (_graphView != null)
        {
            rootVisualElement.Remove(_graphView);
        }
    }

    private void ConstructGraphView()
    {
        _graphView = new LevelBlueprintGraphView
        {
            name = "Blueprint Graph"
        };

        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        _blueprintField = new ObjectField("Blueprint");
        _blueprintField.objectType = typeof(LevelBlueprint);
        _blueprintField.RegisterValueChangedCallback(evt =>
        {
            _currentBlueprint = evt.newValue as LevelBlueprint;
            LoadBlueprint();
        });

        toolbar.Add(_blueprintField);

        var loadButton = new Button(LoadBlueprint) { text = "Load" };
        toolbar.Add(loadButton);

        var saveButton = new Button(SaveBlueprint) { text = "Save" };
        toolbar.Add(saveButton);

        var createNodeButton = new Button(() => { _graphView.CreateNode("New Room", Vector2.zero); }) { text = "Add Node" };
        toolbar.Add(createNodeButton);

        rootVisualElement.Add(toolbar);
    }

    private void LoadBlueprint()
    {
        if (_currentBlueprint == null) return;
        _graphView.PopulateFromBlueprint(_currentBlueprint);
    }

    private void SaveBlueprint()
    {
        if (_currentBlueprint == null)
        {
            Debug.LogWarning("[Blueprint Editor] No blueprint selected to save!");
            return;
        }

        var allNodes = _graphView.nodes.ToList().Cast<LevelBlueprintNode>().ToList();
        
        // Sort to ensure Start node is at index 0 if it exists
        allNodes.Sort((a, b) => 
        {
            RoomNodeType typeA = (RoomNodeType)a.roomTypeField.value;
            RoomNodeType typeB = (RoomNodeType)b.roomTypeField.value;

            if (typeA == RoomNodeType.Start && typeB != RoomNodeType.Start) return -1;
            if (typeA != RoomNodeType.Start && typeB == RoomNodeType.Start) return 1;
            return 0; 
        });

        Dictionary<LevelBlueprintNode, int> nodeToIndex = new Dictionary<LevelBlueprintNode, int>();
        for (int i = 0; i < allNodes.Count; i++) 
        {
            nodeToIndex[allNodes[i]] = i;
        }

        List<NodeBlueprint> newBlueprints = new List<NodeBlueprint>();
        foreach (var node in allNodes) 
        {
            NodeBlueprint bp = new NodeBlueprint();
            bp.nodeName = node.nodeNameField.value;
            bp.roomType = (RoomNodeType)node.roomTypeField.value;
            bp.spawnChance = node.spawnChanceField.value;
            bp.forceDirection = node.forceDirectionField.value;
            bp.requiredDir = (ExitDirection)node.requiredDirField.value;
            bp.position = node.GetPosition().position;

            var outgoingEdges = node.outputPort.connections;
            foreach (var edge in outgoingEdges) 
            {
                var targetNode = edge.input.node as LevelBlueprintNode;
                if (targetNode != null && nodeToIndex.TryGetValue(targetNode, out int childIndex)) 
                {
                    bp.childrenIndices.Add(childIndex);
                }
            }
            newBlueprints.Add(bp);
        }

        _currentBlueprint.nodes = newBlueprints;
        EditorUtility.SetDirty(_currentBlueprint);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Blueprint Editor] Saved blueprint '{_currentBlueprint.name}' with {newBlueprints.Count} nodes.");
    }
}
