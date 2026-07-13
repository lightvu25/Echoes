using UnityEngine;

/// <summary>
/// Represents a node in the HubScene graph.
/// Links to adjacent nodes for logical, trigger-less movement.
/// </summary>
public class HubNode : MonoBehaviour
{
    [Header("Graph Connections")]
    [Tooltip("The node connected in the UP direction.")]
    public HubNode upNode;
    [Tooltip("The node connected in the DOWN direction.")]
    public HubNode downNode;
    [Tooltip("The node connected in the LEFT direction.")]
    public HubNode leftNode;
    [Tooltip("The node connected in the RIGHT direction.")]
    public HubNode rightNode;
}
