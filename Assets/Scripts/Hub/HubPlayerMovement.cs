using UnityEngine;
using System.Collections;

/// <summary>
/// Handles logical, graph-based movement for the player in the HubScene.
/// Completely replaces standard physics-based PlayerMovement and relies
/// on direct component checking instead of colliders/triggers.
/// </summary>
public class HubPlayerMovement : MonoBehaviour
{
    [Header("Graph Movement Settings")]
    [Tooltip("The node the player is currently standing on.")]
    public HubNode currentNode;
    
    [Tooltip("Speed of transition between nodes.")]
    public float moveSpeed = 10f;

    private bool isMoving = false;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (currentNode != null)
        {
            // Snap to current node at start
            transform.position = currentNode.transform.position;
        }
        else
        {
            Debug.LogWarning("[HubPlayerMovement] No starting node assigned!");
        }
    }

    private void Update()
    {
        if (isMoving || currentNode == null) return;

        // 1. Listen for discrete inputs to move along the graph
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryMoveToNode(currentNode.upNode);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            TryMoveToNode(currentNode.downNode);
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TryMoveToNode(currentNode.leftNode);
            // Flip sprite left
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            TryMoveToNode(currentNode.rightNode);
            // Flip sprite right
            transform.localScale = new Vector3(1, 1, 1);
        }
        
        // 2. Handle interactions like opening the Mind Garden UI
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
        {
            // Try to find an interactable on the current node
            if (currentNode.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact();
            }
        }
    }

    private void TryMoveToNode(HubNode targetNode)
    {
        if (targetNode != null)
        {
            StartCoroutine(MoveToNode(targetNode));
        }
    }

    private IEnumerator MoveToNode(HubNode targetNode)
    {
        isMoving = true;

        if (animator != null)
        {
            animator.SetFloat("Speed", 1f); // Start running animation
            animator.SetBool("IsGrounded", true);
        }

        // Move smoothly towards the target node
        while (Vector3.Distance(transform.position, targetNode.transform.position) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetNode.transform.position, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Snap to exact position
        transform.position = targetNode.transform.position;
        currentNode = targetNode;
        
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f); // Return to idle
        }

        // Logical Arrival Check (Trigger-less)
        if (currentNode.TryGetComponent<HubExitNode>(out var exitNode))
        {
            exitNode.TriggerExit();
        }

        isMoving = false;
    }
}
