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
    private EnemyVisual visual;
    private EvolutionManager subscribedManager;

    private void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        sensor = GetComponent<EnemySensor>();
        visual = GetComponent<EnemyVisual>();
    }

    private void OnEnable()
    {
        TrySubscribeToEvolutionManager();
    }

    private void Update()
    {
        if (subscribedManager == null)
        {
            TrySubscribeToEvolutionManager();
        }
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnTierChanged -= HandleTierChanged;
            subscribedManager.OnGlobalAggroTriggered -= HandleGlobalAggro;
            subscribedManager = null;
        }

        visual?.SetSharedVisionIcon(null);
    }

    private void TrySubscribeToEvolutionManager()
    {
        EvolutionManager manager = EvolutionManager.Instance;
        if (manager == null || subscribedManager == manager) return;

        if (subscribedManager != null)
        {
            subscribedManager.OnTierChanged -= HandleTierChanged;
            subscribedManager.OnGlobalAggroTriggered -= HandleGlobalAggro;
        }

        subscribedManager = manager;
        subscribedManager.OnTierChanged += HandleTierChanged;
        subscribedManager.OnGlobalAggroTriggered += HandleGlobalAggro;

        EvolutionTierData currentTier = subscribedManager.GetCurrentTierData();
        if (currentTier != null)
        {
            ApplyTier(currentTier);
        }
    }

    /// <summary>
    /// Called when the global evolution tier changes.
    /// Forwards the new tier data to the brain to overwrite the cloned EnemyData stats.
    /// </summary>
    private void HandleTierChanged(EvolutionTierData newTier)
    {
        ApplyTier(newTier);
    }

    private void ApplyTier(EvolutionTierData tier)
    {
        if (tier == null) return;

        brain.ApplyEvolutionTier(tier);
        visual?.SetSharedVisionIcon(tier.isGlobalAggro ? tier.sharedVisionIcon : null);
    }

    /// <summary>
    /// Called when any enemy spots the player (shared vision).
    /// Checks if this enemy is within the global aggro radius, then triggers aggro.
    /// </summary>
    private void HandleGlobalAggro(Vector3 spotterPosition)
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
        if (subscribedManager != null)
        {
            float radius = subscribedManager.GlobalAggroRadius;
            if (!float.IsPositiveInfinity(radius) && radius > 0f)
            {
                if (Vector3.Distance(transform.position, spotterPosition) > radius)
                {
                    return; // Too far away from the spotter
                }
            }
        }

        sensor.TriggerGlobalAggro();
        brain.ForceNotice();
    }
}
