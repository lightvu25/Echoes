using UnityEngine;
using System.Collections;

/// <summary>
/// Handles logical, graph-based movement for the player in the MindScene.
/// Evaluates NodeConnections, checks for Cut branches, and Challenge Door conditions.
/// </summary>
public class MindPlayerMovement : MonoBehaviour
{
    [Header("Graph Movement Settings")]
    [Tooltip("The node the player is currently standing on.")]
    public MindNode currentNode;
    
    [Tooltip("Speed of transition between nodes.")]
    public float moveSpeed = 10f;

    private bool isMoving = false;
    private Animator animator;

    // To handle continuous inputs as one-shots
    private bool upPressedLastFrame;
    private bool downPressedLastFrame;
    private bool leftPressedLastFrame;
    private bool rightPressedLastFrame;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (currentNode != null)
        {
            transform.position = currentNode.transform.position;
        }
        else
        {
            Debug.LogWarning("[MindPlayerMovement] No starting node assigned!");
        }
    }

    private void Update()
    {
        if (isMoving || currentNode == null || GameInput.Instance == null) return;

        bool upPressed = GameInput.Instance.IsUpActionPressed();
        bool downPressed = GameInput.Instance.IsDownActionPressed();
        bool leftPressed = GameInput.Instance.IsLeftActionPressed();
        bool rightPressed = GameInput.Instance.IsRightActionPressed();

        if (upPressed && !upPressedLastFrame)
        {
            TryMove(currentNode.upConnection);
        }
        else if (downPressed && !downPressedLastFrame)
        {
            TryMove(currentNode.downConnection);
        }
        else if (leftPressed && !leftPressedLastFrame)
        {
            TryMove(currentNode.leftConnection);
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (rightPressed && !rightPressedLastFrame)
        {
            TryMove(currentNode.rightConnection);
            transform.localScale = new Vector3(1, 1, 1);
        }

        upPressedLastFrame = upPressed;
        downPressedLastFrame = downPressed;
        leftPressedLastFrame = leftPressed;
        rightPressedLastFrame = rightPressed;
        
        // Handle Interactions (Mind Garden Altar, Exit, etc.)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
        {
            if (currentNode.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact();
            }
        }
    }

    private void TryMove(NodeConnection connection)
    {
        if (connection == null || connection.targetNode == null) return;

        if (connection.isCut)
        {
            Debug.Log("[MindPlayerMovement] Cannot traverse a cut branch!");
            return;
        }

        MindNode target = connection.targetNode;

        // Challenge Door Checks
        if (target.nodeType == NodeType.ChallengeNoHit)
        {
            int noHitKills = GameSession.Instance?.currentRun?.currentLevelNoHitKills ?? 0;
            if (noHitKills < target.requiredNoHitKills)
            {
                Debug.Log($"[MindPlayerMovement] No-hit door locked. Need {target.requiredNoHitKills} kills without damage. Have: {noHitKills}");
                return;
            }
        }
        else if (target.nodeType == NodeType.ChallengeSpeedrun)
        {
            float levelTime = GameSession.Instance?.currentRun?.currentLevelTime ?? 9999f;
            if (levelTime > target.requiredSpeedrunTime)
            {
                Debug.Log($"[MindPlayerMovement] Speedrun door locked. Needed to reach goal under {target.requiredSpeedrunTime}s. Took: {levelTime}s");
                return;
            }
        }

        StartCoroutine(MoveToNode(target));
    }

    private IEnumerator MoveToNode(MindNode targetNode)
    {
        isMoving = true;

        if (animator != null)
        {
            animator.SetFloat("Speed", 1f); 
            animator.SetBool("IsGrounded", true);
        }

        while (Vector3.Distance(transform.position, targetNode.transform.position) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetNode.transform.position, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetNode.transform.position;
        currentNode = targetNode;
        
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }

        if (currentNode.TryGetComponent<MindExitNode>(out var exitNode))
        {
            exitNode.TriggerExit();
        }

        isMoving = false;
    }
}
