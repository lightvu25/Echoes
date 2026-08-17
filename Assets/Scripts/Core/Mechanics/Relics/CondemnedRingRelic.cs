using UnityEngine;

/// <summary>
/// PoC Relic: Stat Override
/// Max HP is permanently locked to 1 segment.
/// All outgoing attacks are guaranteed Critical Hits.
/// </summary>
public class CondemnedRingRelic : MonoBehaviour, IRelicEffect
{
    private HealthSystem healthSystem;

    public void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID)
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem?.SetMaxHPCap(this, 1);

        // Subscribe to force crits
        eventBus.OnBeforeOutgoingDamage += HandleOutgoingDamage;
    }

    public void OnUnequip(PlayerEventBus eventBus)
    {
        healthSystem?.SetMaxHPCap(this, 0);

        eventBus.OnBeforeOutgoingDamage -= HandleOutgoingDamage;
    }

    private void HandleOutgoingDamage(IDamageable target, ref DamageInfo info)
    {
        if (info.isCritical) return;
        info.isCritical = true;
        info.multiplicativeStack *= healthSystem != null && healthSystem.CombatStats != null
            ? healthSystem.CombatStats.critMultiplier
            : 1.5f;
    }
}
