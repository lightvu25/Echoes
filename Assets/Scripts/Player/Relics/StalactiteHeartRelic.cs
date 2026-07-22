using UnityEngine;

/// <summary>
/// PoC Relic: Fatal Damage Override (Self-Destructing)
/// Upon taking fatal damage: Destroys this Relic, heals 50% HP, and freezes enemies for 3s.
/// </summary>
public class StalactiteHeartRelic : MonoBehaviour, IRelicEffect
{
    private HealthSystem healthSystem;
    private string relicID;

    public void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID)
    {
        healthSystem = GetComponent<HealthSystem>();
        this.relicID = itemID;

        eventBus.OnFatalDamage += HandleFatalDamage;
    }

    public void OnUnequip(PlayerEventBus eventBus)
    {
        eventBus.OnFatalDamage -= HandleFatalDamage;
    }

    private void HandleFatalDamage(ref bool preventDeath)
    {
        // 1. Override death
        preventDeath = true;

        // 2. Heal 50%
        if (healthSystem != null)
        {
            int healAmount = Mathf.FloorToInt(healthSystem.MaxHP * 0.5f);
            healthSystem.Heal(healAmount);
        }

        // 3. Freeze nearby enemies
        // We do a simple OverlapCircle around the player (e.g. 10 unit radius)
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 10f);
        foreach (var col in hitColliders)
        {
            // Assuming enemies have an IEnemyMovement or EnemyMovement script
            // The prompt says: "Enemies have an EnemyMovement script with ApplyRoot(float duration)"
            var enemyMovement = col.GetComponent<IEnemyMovement>();
            if (enemyMovement != null)
            {
                enemyMovement.ApplyRoot(3f);
            }
        }

        // 4. Self-destruct by removing it from the inventory core.
        // This will trigger OnInventoryChanged, which will tell PlayerRelicManager to unequip and destroy this script.
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.RemoveItemByID(this.relicID);
        }
    }
}
