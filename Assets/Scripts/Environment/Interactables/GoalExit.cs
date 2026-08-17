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
            GameObject vfxObj = ObjectPoolManager.SpawnObject(interactVfxPrefab.gameObject, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
            ReturnToPool returnToPool = vfxObj.GetComponent<ReturnToPool>();
            if (returnToPool == null)
            {
                returnToPool = vfxObj.AddComponent<ReturnToPool>();
                returnToPool.ConfigureDelay(2f);
            }
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
