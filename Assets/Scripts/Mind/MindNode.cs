using System.Collections.Generic;
using UnityEngine;

public enum NodeType
{
    Normal,
    Start,
    Reward,
    MindGarden,
    Echo,
    Relic,
    Equipment,
    MapExit,
    ChallengeNoHit,
    ChallengeSpeedrun
}

/// <summary>
/// Represents a node in the MindScene graph.
/// Serves as an interactive object that opens the UI to display risks and rewards.
/// </summary>
public class MindNode : MonoBehaviour, IInteractable, IFeedbackProvider
{
    [Header("Node Settings")]
    public NodeType nodeType = NodeType.Reward;
    
    [Tooltip("If true, arriving at this node will force the player to select a path via UI, cutting the others. If false, the player can just walk freely down any branch without cutting them.")]
    public bool canModifyBranches = true;

    [Tooltip("The icon representing this specific node in the UI.")]
    public Sprite nodeIcon;

    [Tooltip("The modifiers applied to the run if this path is accepted. Leave null for a safe path.")]
    [SerializeField] private MindNodeModifierData modifierData;
    public MindNodeModifierData ModifierData => modifierData;

    private readonly List<ItemBaseData> _featuredItems = new List<ItemBaseData>();
    private bool _featuredItemsRolled;
    public IReadOnlyList<ItemBaseData> FeaturedItems
    {
        get
        {
            EnsureFeaturedItems();
            return _featuredItems;
        }
    }

    [Header("Challenge Requirements")]
    [Tooltip("Enemies killed without taking damage (for No-hit doors)")]
    public int requiredNoHitKills = 30;
    
    [Tooltip("Time limit in seconds to reach the goal (for Speedrun doors)")]
    public float requiredSpeedrunTime = 120f;
    
    [HideInInspector]
    public bool isChallengeClaimed = false;

    [Header("Graph Connections")]
    public MindNodeConnection upConnection;
    public MindNodeConnection downConnection;
    public MindNodeConnection leftConnection;
    public MindNodeConnection rightConnection;

    [Header("Feedback")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2f, 0f);
    public Vector3 PromptOffset => promptOffset;

    public void Interact()
    {
        Debug.Log($"[MindNode] Interacting with {gameObject.name}. Opening Node UI...");
        
        if (UIManager.Instance != null)
        {
            var nodeUI = UIManager.Instance.GetPanel<MindNodeUI>(UIPanelType.MindNode);
            if (nodeUI != null)
            {
                nodeUI.DisplayNode(this);
                UIManager.Instance.OpenPanel(UIPanelType.MindNode);
            }
            else
            {
                Debug.LogWarning("[MindNode] Could not find MindNodeUI via UIManager!");
            }
        }
    }

    public void EnsureFeaturedItems()
    {
        if (_featuredItemsRolled) return;
        _featuredItemsRolled = true;

        if (modifierData == null || !modifierData.useFeaturedItemBoost || !TryGetRewardCategory(out ItemCategory category))
            return;

        _featuredItems.AddRange(modifierData.RollFeaturedItems(category));
    }

    public bool TryGetRewardCategory(out ItemCategory category)
    {
        switch (nodeType)
        {
            case NodeType.Relic:
                category = ItemCategory.Relic;
                return true;
            case NodeType.Echo:
                category = ItemCategory.Echo;
                return true;
            case NodeType.Equipment:
                category = ItemCategory.Tool;
                return true;
            default:
                category = default;
                return false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        DrawConnectionGizmo(upConnection, Vector3.up);
        DrawConnectionGizmo(downConnection, Vector3.down);
        DrawConnectionGizmo(leftConnection, Vector3.left);
        DrawConnectionGizmo(rightConnection, Vector3.right);
    }

    private void DrawConnectionGizmo(MindNodeConnection connection, Vector3 dirOffset)
    {
        if (connection != null && connection.targetNode != null)
        {
            if (connection.isCut) Gizmos.color = Color.red;
            else if (connection.isConnected) Gizmos.color = Color.green;
            else Gizmos.color = Color.cyan;

            Vector3 startPos = transform.position + dirOffset * 0.2f;
            Vector3 endPos = connection.targetNode.transform.position;
            Gizmos.DrawLine(startPos, endPos);
            
            // Draw a small sphere at the end to indicate direction
            Gizmos.DrawSphere(Vector3.Lerp(startPos, endPos, 0.9f), 0.15f);
        }
    }
#endif
}

[System.Serializable]
public class MindNodeConnection
{
    [Tooltip("The target MindNode this branch connects to.")]
    public MindNode targetNode;

    [Tooltip("Is this branch physically cut by the player?")]
    public bool isCut;

    [Tooltip("Is this branch manually connected by the player?")]
    public bool isConnected;
}
