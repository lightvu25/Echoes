using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns LineRenderer GameObjects with MindBranchInteraction for every valid graph connection in the scene at runtime.
/// </summary>
public class MindWorldRenderer : MonoBehaviour
{
    [Header("Line Settings")]
    [Tooltip("The width of the path lines.")]
    public float lineWidth = 0.5f;

    [Tooltip("The Material to use for the lines. If null, uses Sprites-Default.")]
    public Material lineMaterial;
    
    [Tooltip("Color of a connected, traversed, or active path.")]
    public Color activeColor = Color.cyan;
    
    [Tooltip("Color of a path that has been cut.")]
    public Color cutColor = new Color(1f, 0f, 0f, 0.2f); // Faint red

    private Dictionary<(MindNode, MindNode), LineRenderer> branchVisuals = new Dictionary<(MindNode, MindNode), LineRenderer>();

    private void Start()
    {
        GenerateAllPaths();
    }

    public void RefreshAllPaths()
    {
        MindNode[] allNodes = FindObjectsByType<MindNode>(FindObjectsSortMode.None);
        foreach (MindNode node in allNodes)
        {
            UpdateConnectionVisual(node, node.upConnection);
            UpdateConnectionVisual(node, node.downConnection);
            UpdateConnectionVisual(node, node.leftConnection);
            UpdateConnectionVisual(node, node.rightConnection);
        }
    }

    private void UpdateConnectionVisual(MindNode source, MindNodeConnection connection)
    {
        if (connection == null || connection.targetNode == null) return;
        
        MindNode target = connection.targetNode;
        (MindNode, MindNode) pair = source.GetInstanceID() < target.GetInstanceID() 
            ? (source, target) 
            : (target, source);

        if (branchVisuals.TryGetValue(pair, out LineRenderer lr))
        {
            if (connection.isCut)
            {
                lr.startColor = cutColor;
                lr.endColor = cutColor;
            }
            else
            {
                lr.startColor = activeColor;
                lr.endColor = activeColor;
            }
        }
    }

    private void GenerateAllPaths()
    {
        MindNode[] allNodes = FindObjectsByType<MindNode>(FindObjectsSortMode.None);
        Debug.Log($"[MindWorldRenderer] GenerateAllPaths found {allNodes.Length} nodes.");
        
        // Keep track of connections we already drew so we don't draw overlapping two-way lines
        HashSet<(MindNode, MindNode)> drawnConnections = new HashSet<(MindNode, MindNode)>();

        foreach (MindNode node in allNodes)
        {
            TryDrawConnection(node, node.upConnection, drawnConnections);
            TryDrawConnection(node, node.downConnection, drawnConnections);
            TryDrawConnection(node, node.leftConnection, drawnConnections);
            TryDrawConnection(node, node.rightConnection, drawnConnections);
        }
    }

    private void TryDrawConnection(MindNode source, MindNodeConnection connection, HashSet<(MindNode, MindNode)> drawnConnections)
    {
        if (connection == null || connection.targetNode == null) return;
        
        MindNode target = connection.targetNode;

        // Ensure we always order the tuple consistently so A->B and B->A are the same connection
        (MindNode, MindNode) pair = source.GetInstanceID() < target.GetInstanceID() 
            ? (source, target) 
            : (target, source);

        if (drawnConnections.Contains(pair)) return;
        drawnConnections.Add(pair);

        SpawnBranchVisuals(source, connection, pair);
    }

    private void SpawnBranchVisuals(MindNode source, MindNodeConnection connection, (MindNode, MindNode) pair)
    {
        MindNode target = connection.targetNode;

        GameObject branchObj = new GameObject($"Branch_{source.name}_to_{target.name}");
        branchObj.transform.SetParent(this.transform);

        LineRenderer lr = branchObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, source.transform.position);
        lr.SetPosition(1, target.transform.position);
        
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.numCapVertices = 4;
        lr.sortingOrder = -5; // Push behind Player and Nodes

        if (lineMaterial != null)
        {
            lr.material = lineMaterial;
        }
        else
        {
            // Default Unity Sprite Material fallback
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        if (connection.isCut)
        {
            lr.startColor = cutColor;
            lr.endColor = cutColor;
        }
        else
        {
            lr.startColor = activeColor;
            lr.endColor = activeColor;
        }

        branchVisuals[pair] = lr;
    }
}
