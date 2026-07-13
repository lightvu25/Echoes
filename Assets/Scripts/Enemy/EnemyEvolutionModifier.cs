using UnityEngine;

/// <summary>
/// Per-enemy bridge component that listens to EvolutionManager events
/// and applies evolution tier changes and global aggro to its EnemyBrain and EnemySensor.
/// </summary>
[RequireComponent(typeof(EnemyBrain), typeof(EnemySensor))]
public class EnemyEvolutionModifier : MonoBehaviour
{
    private EnemyBrain brain;
    private EnemySensor sensor;

    private void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        sensor = GetComponent<EnemySensor>();
    }

    private void OnEnable()
    {
        if (EvolutionManager.Instance != null)
        {
            EvolutionManager.Instance.OnTierChanged += HandleTierChanged;
            EvolutionManager.Instance.OnGlobalAggroTriggered += HandleGlobalAggro;

            // Late-spawn support: immediately apply the current tier
            EvolutionTierData currentTier = EvolutionManager.Instance.GetCurrentTierData();
            if (currentTier != null)
            {
                brain.ApplyEvolutionTier(currentTier);
            }
        }
    }

    private void OnDisable()
    {
        if (EvolutionManager.Instance != null)
        {
            EvolutionManager.Instance.OnTierChanged -= HandleTierChanged;
            EvolutionManager.Instance.OnGlobalAggroTriggered -= HandleGlobalAggro;
        }
    }

    /// <summary>
    /// Called when the global evolution tier changes.
    /// Forwards the new tier data to the brain to overwrite the cloned EnemyData stats.
    /// </summary>
    private void HandleTierChanged(EvolutionTierData newTier)
    {
        brain.ApplyEvolutionTier(newTier);
    }

    /// <summary>
    /// Called when any enemy spots the player (shared vision).
    /// Checks if this enemy is within the global aggro radius, then triggers aggro.
    /// </summary>
    private void HandleGlobalAggro()
    {
        // Already chasing or attacking — no need to override
        if (brain.CurrentState == EnemyBrain.State.Chase
            || brain.CurrentState == EnemyBrain.State.Telegraph
            || brain.CurrentState == EnemyBrain.State.Attack
            || brain.CurrentState == EnemyBrain.State.Notice)
        {
            return;
        }

        // Radius filtering: only aggro enemies within the configured radius
        if (EvolutionManager.Instance != null)
        {
            float radius = EvolutionManager.Instance.GlobalAggroRadius;
            if (!float.IsPositiveInfinity(radius))
            {
                // If the spotter's position matters for radius checks,
                // the manager would need to pass it. For now, all enemies react.
                // This is a placeholder for future radius-based filtering.
            }
        }

        sensor.TriggerGlobalAggro();
        brain.ForceNotice();
    }
}
