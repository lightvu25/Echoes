#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MindWorldEditor : EditorWindow
{
    [MenuItem("Tools/Echoes/Mind World Auto-Connect")]
    public static void ShowWindow()
    {
        GetWindow<MindWorldEditor>("Mind World Setup");
    }

    private float maxConnectionDistance = 15f;
    private float angleTolerance = 30f; // degrees

    private void OnGUI()
    {
        GUILayout.Label("Mind Node Auto-Connector", EditorStyles.boldLabel);
        
        maxConnectionDistance = EditorGUILayout.FloatField("Max Distance", maxConnectionDistance);
        angleTolerance = EditorGUILayout.FloatField("Angle Tolerance", angleTolerance);

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-Connect All Nodes"))
        {
            AutoConnectNodes();
        }
        
        if (GUILayout.Button("Clear All Connections"))
        {
            ClearAllConnections();
        }
    }

    private void AutoConnectNodes()
    {
        MindNode[] allNodes = FindObjectsOfType<MindNode>();
        if (allNodes.Length == 0)
        {
            Debug.LogWarning("No MindNodes found in the scene.");
            return;
        }

        Undo.RecordObjects(allNodes, "Auto-Connect Mind Nodes");

        int connectionsMade = 0;

        foreach (MindNode node in allNodes)
        {
            // Initialize connections if null
            if (node.upConnection == null) node.upConnection = new MindNodeConnection();
            if (node.downConnection == null) node.downConnection = new MindNodeConnection();
            if (node.leftConnection == null) node.leftConnection = new MindNodeConnection();
            if (node.rightConnection == null) node.rightConnection = new MindNodeConnection();

            // Clear existing targets first so we can safely recalculate
            node.upConnection.targetNode = null;
            node.downConnection.targetNode = null;
            node.leftConnection.targetNode = null;
            node.rightConnection.targetNode = null;

            MindNode bestUp = null; float distUp = maxConnectionDistance;
            MindNode bestDown = null; float distDown = maxConnectionDistance;
            MindNode bestLeft = null; float distLeft = maxConnectionDistance;
            MindNode bestRight = null; float distRight = maxConnectionDistance;

            foreach (MindNode otherNode in allNodes)
            {
                if (node == otherNode) continue;

                Vector3 dir = otherNode.transform.position - node.transform.position;
                float dist = dir.magnitude;

                if (dist > maxConnectionDistance) continue;

                dir.Normalize();
                
                // Check Up
                if (Vector3.Angle(dir, Vector3.up) <= angleTolerance && dist < distUp)
                {
                    bestUp = otherNode; distUp = dist;
                }
                // Check Down
                else if (Vector3.Angle(dir, Vector3.down) <= angleTolerance && dist < distDown)
                {
                    bestDown = otherNode; distDown = dist;
                }
                // Check Left
                else if (Vector3.Angle(dir, Vector3.left) <= angleTolerance && dist < distLeft)
                {
                    bestLeft = otherNode; distLeft = dist;
                }
                // Check Right
                else if (Vector3.Angle(dir, Vector3.right) <= angleTolerance && dist < distRight)
                {
                    bestRight = otherNode; distRight = dist;
                }
            }

            if (bestUp != null) { node.upConnection.targetNode = bestUp; connectionsMade++; }
            if (bestDown != null) { node.downConnection.targetNode = bestDown; connectionsMade++; }
            if (bestLeft != null) { node.leftConnection.targetNode = bestLeft; connectionsMade++; }
            if (bestRight != null) { node.rightConnection.targetNode = bestRight; connectionsMade++; }
            
            EditorUtility.SetDirty(node);
        }

        Debug.Log($"[MindWorldEditor] Auto-Connect complete! Made {connectionsMade} valid connections.");
    }

    private void ClearAllConnections()
    {
        MindNode[] allNodes = FindObjectsOfType<MindNode>();
        Undo.RecordObjects(allNodes, "Clear Mind Node Connections");

        foreach (MindNode node in allNodes)
        {
            if (node.upConnection != null) node.upConnection.targetNode = null;
            if (node.downConnection != null) node.downConnection.targetNode = null;
            if (node.leftConnection != null) node.leftConnection.targetNode = null;
            if (node.rightConnection != null) node.rightConnection.targetNode = null;
            EditorUtility.SetDirty(node);
        }
        
        Debug.Log("[MindWorldEditor] Cleared all connections.");
    }
}
#endif
