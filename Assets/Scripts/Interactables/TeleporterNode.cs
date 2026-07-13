using UnityEngine;

/// <summary>
/// A fast travel node inspired by Dead Cells. 
/// Unlocks permanently upon first proximity, allowing interaction via InteractableTrigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(InteractableTrigger))]
public class TeleporterNode : MonoBehaviour, IInteractable
{
    [Header("Teleporter Settings")]
    public string nodeName = "Unknown Teleporter";
    [Header("Visuals (Unlock)")]
    [Tooltip("Particles played once when the node is first discovered.")]
    [SerializeField] private ParticleSystem unlockParticles;
    
    [Tooltip("The sprite renderer that represents this teleporter's physical body.")]
    [SerializeField] private SpriteRenderer visualRenderer;
    
    [Tooltip("The glowing material to swap to when unlocked.")]
    [SerializeField] private Material glowingMaterial;

    [Header("Minimap")]
    [Tooltip("The minimap icon object to enable when this teleporter is unlocked.")]
    [SerializeField] private GameObject minimapIcon;

    private bool isUnlocked = false;
    private InteractableTrigger interactableTrigger;

    private void Awake()
    {
        interactableTrigger = GetComponent<InteractableTrigger>();
        // Disable the interactable trigger until the node is unlocked
        if (interactableTrigger != null)
        {
            interactableTrigger.enabled = false;
        }

        // Hide minimap icon initially
        if (minimapIcon != null)
        {
            minimapIcon.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only unlock if the player walks by for the first time
        if (!isUnlocked && collision.CompareTag("Player"))
        {
            UnlockNode();
        }
    }

    private void UnlockNode()
    {
        isUnlocked = true;

        // Play unlock VFX
        if (unlockParticles != null)
        {
            unlockParticles.Play();
        }

        // Swap material to glowing state
        if (visualRenderer != null && glowingMaterial != null)
        {
            visualRenderer.material = glowingMaterial;
        }

        // Enable the trigger so the player can now press 'F' to interact
        if (interactableTrigger != null)
        {
            interactableTrigger.enabled = true;
        }

        // Show minimap icon
        if (minimapIcon != null)
        {
            minimapIcon.SetActive(true);
        }

        // Register with the global manager
        if (TeleportManager.Instance != null)
        {
            TeleportManager.Instance.RegisterNode(this);
        }
        else
        {
            Debug.LogWarning("[TeleporterNode] TeleportManager instance not found! Make sure it exists in the scene.");
        }
    }

    /// <summary>
    /// Implements IInteractable.
    /// This is called when the player presses the interact key ('F') while in range of the InteractableTrigger.
    /// </summary>
    public void Interact()
    {
        if (!isUnlocked) return;

        // Open the Map UI where the player can select a destination
        if (UIManager.Instance != null)
        {
            if (TeleportManager.Instance != null)
            {
                TeleportManager.Instance.CurrentActiveNode = this;
            }
            UIManager.Instance.OpenPanel(UIPanelType.Map);
        }
        else
        {
            Debug.LogWarning("[TeleporterNode] UIManager instance not found! Cannot open Map UI.");
        }
    }
}
