using UnityEngine;

/// <summary>
/// Migrated Relic: Grants gold on hit when player is at 1 HP.
/// </summary>
public class DyingAmuletRelic : MonoBehaviour, IRelicEffect
{
    private HealthSystem healthSystem;

    public void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID)
    {
        healthSystem = GetComponent<HealthSystem>();
        eventBus.OnBeforeOutgoingDamage += HandleOutgoingDamage;
    }

    public void OnUnequip(PlayerEventBus eventBus)
    {
        eventBus.OnBeforeOutgoingDamage -= HandleOutgoingDamage;
    }

    private void HandleOutgoingDamage(IDamageable target, ref DamageInfo info)
    {
        if (healthSystem != null && healthSystem.CurrentHP <= 1)
        {
            // Award gold (this achieves the mechanic of spawning gold on hit)
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddGold(1);
            }
        }
    }
}
