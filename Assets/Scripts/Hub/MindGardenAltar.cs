using UnityEngine;

/// <summary>
/// Central altar in the HubScene that opens the Mind Garden upgrade UI.
/// Implements IInteractable and IFeedbackProvider to match the existing
/// interaction system used by Chest and other interactables.
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

        // Spawn optional VFX
        if (interactEffect != null)
        {
            Instantiate(interactEffect, transform.position + Vector3.up, Quaternion.identity);
        }

        // TODO: Open the Mind Garden upgrade panel.
        // This will be expanded to show the Memory Web UI, allowing the player
        // to spend Echoes currency on permanent upgrades and unlock new paths.
    }
}
