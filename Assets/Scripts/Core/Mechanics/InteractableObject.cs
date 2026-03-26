using UnityEngine;

/// <summary>
/// Generic base class for interactable objects (NPCs, chests, statues, dropped items).
/// Handles player proximity detection, toggling the UI prompt, and reading the Interact input.
/// </summary>
public abstract class InteractableObject : MonoBehaviour
{
    [Header("Base Interface")]
    [Tooltip("Child GameObject containing the interact prompt (e.g., UI button popup).")]
    [SerializeField] protected GameObject interactPrompt;

    protected bool isPlayerInRange = false;

    protected virtual void Update()
    {
        if (!isPlayerInRange) return;

        // Interaction is mapped to the "Up" action in this project
        // (as seen in PlayerInteract.cs statue interaction logic).
        if (GameInput.Instance != null && GameInput.Instance.IsUpActionPressed())
        {
            OnInteract();
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            SetPromptVisible(true);
            OnPlayerEnter(other);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            SetPromptVisible(false);
            OnPlayerExit(other);
        }
    }

    /// <summary>
    /// Called when the player is in range and triggers the interaction input.
    /// Override in subclasses to define the specific behavior.
    /// </summary>
    protected abstract void OnInteract();

    /// <summary>Optional hook for when the player enters interaction range.</summary>
    protected virtual void OnPlayerEnter(Collider2D player) { }

    /// <summary>Optional hook for when the player exits interaction range.</summary>
    protected virtual void OnPlayerExit(Collider2D player) { }

    protected virtual void SetPromptVisible(bool visible)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(visible);
        }
    }
}
