using System.Collections.Generic;
using UnityEngine;

public class TeleportManager : MonoBehaviour
{
    public static TeleportManager Instance { get; private set; }

    [Tooltip("List of teleporter nodes the player has discovered.")]
    public List<TeleporterNode> unlockedNodes = new List<TeleporterNode>();
    
    [HideInInspector]
    public TeleporterNode CurrentActiveNode { get; set; }

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

    public void RegisterNode(TeleporterNode node)
    {
        if (node != null && !unlockedNodes.Contains(node))
        {
            unlockedNodes.Add(node);
            Debug.Log($"[TeleportManager] Registered new node: {node.nodeName}");
        }
    }

    public void TeleportPlayerTo(TeleporterNode targetNode, Transform playerTransform)
    {
        if (targetNode == null || playerTransform == null) return;

        Vector3 safePosition = targetNode.transform.position + (Vector3.up * 0.2f);

        playerTransform.position = safePosition;

        Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = safePosition; 
        }

        Physics2D.SyncTransforms();

        Debug.Log($"[TeleportManager] Teleported player to {targetNode.nodeName} at {safePosition}");
        Time.timeScale = 1f;
    }
}
