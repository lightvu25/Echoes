using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for the Fast Travel / Teleporter system.
/// Keeps track of unlocked nodes and handles the teleportation logic.
/// </summary>
public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance { get; private set; }

    [Tooltip("List of teleporter nodes the player has discovered.")]
    public List<TeleporterNode> unlockedNodes = new List<TeleporterNode>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Registers a newly discovered teleporter node.
    /// </summary>
    public void RegisterNode(TeleporterNode node)
    {
        if (node != null && !unlockedNodes.Contains(node))
        {
            unlockedNodes.Add(node);
            Debug.Log($"[TeleportManager] Registered new node: {node.nodeName}");
        }
    }

    /// <summary>
    /// Teleports the player to the target node's spawn position safely.
    /// </summary>
    public void TeleportPlayerTo(TeleporterNode targetNode, Transform playerTransform)
    {
        if (targetNode == null || playerTransform == null) return;

        // Safely reset velocity to prevent physics glitches after teleporting
        Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Move player
        playerTransform.position = targetNode.transform.position;
        
        Debug.Log($"[TeleportManager] Teleported player to {targetNode.nodeName}");
        
        // Unpause game if it was paused by UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseCurrentPanel();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
}
