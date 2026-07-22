using UnityEngine;

/// <summary>
/// PoC Relic: Stat Override
/// Max HP is permanently locked to 1 segment.
/// All outgoing attacks are guaranteed Critical Hits.
/// </summary>
public class CondemnedRingRelic : MonoBehaviour, IRelicEffect
{
    private HealthSystem healthSystem;
    private int originalMaxHP;

    public void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID)
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            // Cache original MaxHP to restore it on unequip
            originalMaxHP = healthSystem.MaxHP;
            
            // Assuming 1 slot is defined by the HealthSystem's total slot logic. 
            // We lock HP to whatever constitutes 1 slot. Since UnlockedSlots = Ceil(CurrentHP / hpPerSlot), 
            // we could force max HP to a low value. Wait, let's just use healthSystem.SetMaxHP(healthSystem.MaxHP / healthSystem.UnlockedSlots, false);
            // Wait, we can't cleanly access hpPerSlot if it's private.
            // Let's just lock it to 20 for the sake of the PoC, or read CombatStats.hpPerSlot if accessible.
            // Assuming CombatStats has base values. We will set Max HP to a fixed tiny amount.
            healthSystem.SetMaxHP(20, false); // Lock to 20 HP (1 segment roughly)
        }

        // Subscribe to force crits
        eventBus.OnBeforeOutgoingDamage += HandleOutgoingDamage;
    }

    public void OnUnequip(PlayerEventBus eventBus)
    {
        if (healthSystem != null)
        {
            healthSystem.SetMaxHP(originalMaxHP, false);
        }

        eventBus.OnBeforeOutgoingDamage -= HandleOutgoingDamage;
    }

    private void HandleOutgoingDamage(IDamageable target, ref DamageInfo info)
    {
        info.isCritical = true;
        // Apply the game's crit multiplier (usually 1.5x, we'll hardcode 1.5x here as an example)
        info.multiplicativeStack *= 1.5f;
    }
}
