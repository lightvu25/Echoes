using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class LevelBlueprintGraphView : GraphView
{
    public LevelBlueprintGraphView()
    {
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Add a grid background
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    public void ClearGraph()
    {
        graphElements.ForEach(RemoveElement);
    }

    public LevelBlueprintNode CreateNode(string nodeName, Vector2 position)
    {
        var node = new LevelBlueprintNode();
        node.title = string.IsNullOrEmpty(nodeName) ? "New Room" : nodeName;
        node.nodeNameField.value = node.title;
        node.SetPosition(new Rect(position, new Vector2(200, 150)));
        AddElement(node);
        return node;
    }

    public void PopulateFromBlueprint(LevelBlueprint blueprint)
    {
        ClearGraph();

        if (blueprint == null || blueprint.nodes == null)
            return;

        Dictionary<int, LevelBlueprintNode> nodeDict = new Dictionary<int, LevelBlueprintNode>();

        // Create nodes
        for (int i = 0; i < blueprint.nodes.Count; i++)
        {
            var bpNode = blueprint.nodes[i];
            var node = CreateNode(bpNode.nodeName, bpNode.position);
            
            node.roomTypeField.value = bpNode.roomType;
            node.spawnChanceField.value = bpNode.spawnChance;
            node.forceDirectionField.value = bpNode.forceDirection;
            node.requiredDirField.value = bpNode.requiredDir;

            nodeDict[i] = node;
        }

        // Connect nodes
        for (int i = 0; i < blueprint.nodes.Count; i++)
        {
            var bpNode = blueprint.nodes[i];
            if (bpNode.childrenIndices != null)
            {
                foreach (int childIndex in bpNode.childrenIndices)
                {
                    if (nodeDict.ContainsKey(i) && nodeDict.ContainsKey(childIndex))
                    {
                        var parentNode = nodeDict[i];
                        var childNode = nodeDict[childIndex];

                        var edge = parentNode.outputPort.ConnectTo(childNode.inputPort);
                        AddElement(edge);
                    }
                }
            }
        }
    }
}
