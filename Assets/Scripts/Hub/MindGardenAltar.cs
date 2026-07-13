using UnityEngine;

/// <summary>
/// Central altar in the MindScene that opens the Mind Garden upgrade UI.
/// Provides backend logic for cutting and connecting branches.
/// </summary>
public class MindGardenAltar : MonoBehaviour, IInteractable, IFeedbackProvider
{
    [Header("Feedback")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 2f, 0f);

    [Header("Visual Effects")]
    [SerializeField] private GameObject interactEffect;

    // IFeedbackProvider
    public Vector3 PromptOffset => promptOffset;

    // IInteractable
    public void Interact()
    {
        Debug.Log("[MindGardenAltar] Opening Mind Garden Upgrade UI...");

        if (interactEffect != null)
        {
            Instantiate(interactEffect, transform.position + Vector3.up, Quaternion.identity);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPanel(UIPanelType.MindGarden);
        }
    }

    /// <summary>
    /// Backend method to be called by the UI when a branch is CUT.
    /// Increases Magic Toxicity.
    /// </summary>
    public void CutBranch(NodeConnection connection)
    {
        if (connection == null || connection.isCut) return;

        connection.isCut = true;
        connection.isConnected = false;

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.magicToxicity += connection.magicToxicityCost;
            Debug.Log($"[MindGardenAltar] Cut branch to {connection.targetNode.name}. Magic Toxicity increased to {GameSession.Instance.currentRun.magicToxicity}.");
        }
    }

    /// <summary>
    /// Backend method to be called by the UI when a branch is CONNECTED.
    /// Increases Relic Bonus and base difficulty.
    /// </summary>
    public void ConnectBranch(NodeConnection connection)
    {
        if (connection == null || connection.isConnected) return;

        connection.isConnected = true;
        connection.isCut = false;

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.relicBonusModifier += connection.relicBonusPercentage;
            Debug.Log($"[MindGardenAltar] Connected branch to {connection.targetNode.name}. Relic Bonus increased to {GameSession.Instance.currentRun.relicBonusModifier}.");
        }
    }
}
