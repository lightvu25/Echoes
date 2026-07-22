using UnityEngine;

public class GoalExit : MonoBehaviour, IInteractable
{
    [Header("Visuals")]
    [Tooltip("Optional VFX to spawn when interacting with the goal")]
    [SerializeField] private Transform interactVfxPrefab;

    public void Interact()
    {
        // Spawn visual effects if assigned
        if (interactVfxPrefab != null)
        {
            Transform vfx = Instantiate(interactVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx.gameObject, 2f); // Clean up after a few seconds
        }

        // Transition to the next level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerLevelTransition(transform.position);
        }
        else
        {
            Debug.LogWarning("GoalExit: GameManager instance not found, unable to transition!");
        }
    }
}
