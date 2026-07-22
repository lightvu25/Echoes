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

    private Transform visualTransform;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            visualTransform = animator.transform;
        }
    }

    [Header("UI Prompts")]
    [Tooltip("Assign the W sprite object here (child of player)")]
    public GameObject promptW;
    [Tooltip("Assign the S sprite object here")]
    public GameObject promptS;
    [Tooltip("Assign the A sprite object here")]
    public GameObject promptA;
    [Tooltip("Assign the D sprite object here")]
    public GameObject promptD;

    private void Start()
    {
        if (currentNode != null)
        {
            transform.position = currentNode.transform.position;
            UpdatePrompts();
        }
        else
        {
            Debug.LogWarning("[MindPlayerMovement] No starting node assigned!");
        }
    }

    private void Update()
    {
        if (isMoving || currentNode == null || GameInput.Instance == null) return;
        if (UIManager.Instance != null && (UIManager.Instance.IsTimeFrozenByPanel || UIManager.Instance.WasPanelClosedThisFrame)) return;

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
            if (visualTransform != null) visualTransform.localScale = new Vector3(-1, 1, 1);
        }
        else if (rightPressed && !rightPressedLastFrame)
        {
            TryMove(currentNode.rightConnection);
            if (visualTransform != null) visualTransform.localScale = new Vector3(1, 1, 1);
        }

        upPressedLastFrame = upPressed;
        downPressedLastFrame = downPressed;
        leftPressedLastFrame = leftPressed;
        rightPressedLastFrame = rightPressed;
        
        // Handle Interactions (Mind Garden Altar, Exit, etc.)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space))
        {
            // Do NOT interact if a blocking UI is currently open
            if (UIManager.Instance != null && 
               (UIManager.Instance.CurrentActivePanel == UIPanelType.MindNodeSelection || 
                UIManager.Instance.CurrentActivePanel == UIPanelType.MindGarden ||
                UIManager.Instance.CurrentActivePanel == UIPanelType.MindNode))
            {
                return;
            }

            if (currentNode.TryGetComponent<MindExitNode>(out var exitNode))
            {
                exitNode.Interact();
            }
            else if (currentNode.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact();
            }
        }
    }

    private void TryMove(MindNodeConnection connection)
    {
        if (!CanMove(connection)) return;

        MindNode target = connection.targetNode;
        StartCoroutine(MoveToNode(target));
    }

    private bool CanMove(MindNodeConnection connection)
    {
        if (connection == null || connection.targetNode == null) return false;

        if (connection.isCut)
        {
            return false;
        }

        // Challenge Door Checks - apply to leaving the current node, not entering the target node!
        if (currentNode.nodeType == NodeType.ChallengeNoHit)
        {
            int noHitKills = GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.currentLevelNoHitKills : 0;
            if (noHitKills < currentNode.requiredNoHitKills)
            {
                // Locked in! But we don't return false because we don't want to trap the player.
                // The branches are locked via TriggerSelectionUIIfNeeded instead.
            }
        }
        else if (currentNode.nodeType == NodeType.ChallengeSpeedrun)
        {
            float levelTime = GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.currentLevelTime : 9999f;
            if (levelTime > currentNode.requiredSpeedrunTime)
            {
                // Locked in! But we don't return false because we don't want to trap the player.
                // The branches are locked via TriggerSelectionUIIfNeeded instead.
            }
        }

        return true;
    }

    private IEnumerator MoveToNode(MindNode targetNode)
    {
        isMoving = true;
        HideAllPrompts();

        // Auto-Hide UIs if they were open from standing on the previous node
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePanelIfOpen(UIPanelType.MindNode);
            UIManager.Instance.ClosePanelIfOpen(UIPanelType.MindGarden);
        }

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

        isMoving = false;
        
        // Apply Modifiers when arriving at the node
        if (MindPathManager.Instance != null && currentNode.ModifierData != null)
        {
            MindPathManager.Instance.AcceptNodePath(currentNode);
        }

        UpdatePrompts();
        TriggerSelectionUIIfNeeded();
    }

    public void TriggerSelectionUIIfNeeded()
    {
        if (currentNode == null) return;
        
        // --- Auto-Display UIs for specific node types ---
        if (currentNode.nodeType == NodeType.Echo || 
            currentNode.nodeType == NodeType.Relic || 
            currentNode.nodeType == NodeType.Equipment || 
            currentNode.nodeType == NodeType.MapExit ||
            currentNode.nodeType == NodeType.ChallengeNoHit ||
            currentNode.nodeType == NodeType.ChallengeSpeedrun)
        {
            if (UIManager.Instance != null)
            {
                var nodeUI = UIManager.Instance.GetPanel<MindNodeUI>(UIPanelType.MindNode);
                if (nodeUI != null)
                {
                    nodeUI.DisplayNode(currentNode);
                    UIManager.Instance.OpenPanel(UIPanelType.MindNode);
                }
            }
        }
        else if (currentNode.nodeType == NodeType.MindGarden)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel(UIPanelType.MindGarden);
            }
        }

        // --- Handle Node Selection UI (Branch Modification) ---
        if (!currentNode.canModifyBranches) return;

        // Do not allow modifying branches if the challenge is failed!
        if (currentNode.nodeType == NodeType.ChallengeSpeedrun)
        {
            float levelTime = GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.currentLevelTime : 9999f;
            if (levelTime > currentNode.requiredSpeedrunTime) return;
        }
        else if (currentNode.nodeType == NodeType.ChallengeNoHit)
        {
            int noHitKills = GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.currentLevelNoHitKills : 0;
            if (noHitKills < currentNode.requiredNoHitKills) return;
        }

        var connections = new System.Collections.Generic.List<MindNodeConnection>();
        if (currentNode.upConnection != null && currentNode.upConnection.targetNode != null && currentNode.upConnection.targetNode.nodeType != NodeType.MapExit) connections.Add(currentNode.upConnection);
        if (currentNode.downConnection != null && currentNode.downConnection.targetNode != null && currentNode.downConnection.targetNode.nodeType != NodeType.MapExit) connections.Add(currentNode.downConnection);
        if (currentNode.leftConnection != null && currentNode.leftConnection.targetNode != null && currentNode.leftConnection.targetNode.nodeType != NodeType.MapExit) connections.Add(currentNode.leftConnection);
        if (currentNode.rightConnection != null && currentNode.rightConnection.targetNode != null && currentNode.rightConnection.targetNode.nodeType != NodeType.MapExit) connections.Add(currentNode.rightConnection);

        if (connections.Count > 1)
        {
            if (UIManager.Instance != null)
            {
                var selectionUI = UIManager.Instance.GetPanel<MindNodeSelectionUI>(UIPanelType.MindNodeSelection);
                if (selectionUI != null)
                {
                    UIManager.Instance.OpenPanel(UIPanelType.MindNodeSelection);
                    selectionUI.SetupSelection(currentNode, connections);
                }
            }
        }
    }

    private void UpdatePrompts()
    {
        if (currentNode == null) return;
        
        if (promptW != null) promptW.SetActive(CanMove(currentNode.upConnection));
        if (promptS != null) promptS.SetActive(CanMove(currentNode.downConnection));
        if (promptA != null) promptA.SetActive(CanMove(currentNode.leftConnection));
        if (promptD != null) promptD.SetActive(CanMove(currentNode.rightConnection));
    }

    private void HideAllPrompts()
    {
        if (promptW != null) promptW.SetActive(false);
        if (promptS != null) promptS.SetActive(false);
        if (promptA != null) promptA.SetActive(false);
        if (promptD != null) promptD.SetActive(false);
    }
}
