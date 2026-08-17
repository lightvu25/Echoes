using UnityEngine;

/// <summary>
/// Migrated Relic: 3x multiplicativeStack when hitting a knocked-back enemy.
/// </summary>
public class CompensatingSawRelic : MonoBehaviour, IRelicEffect
{
    public void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID)
    {
        eventBus.OnBeforeOutgoingDamage += HandleOutgoingDamage;
    }

    public void OnUnequip(PlayerEventBus eventBus)
    {
        eventBus.OnBeforeOutgoingDamage -= HandleOutgoingDamage;
    }

    private void HandleOutgoingDamage(IDamageable target, ref DamageInfo info)
    {
        if (target != null && target.Transform != null)
        {
            var enemyMovement = target.Transform.GetComponent<IEnemyMovement>();
            if (enemyMovement != null && enemyMovement.IsKnockedBack)
            {
                info.multiplicativeStack *= 3.0f;
            }
        }
    }
}
