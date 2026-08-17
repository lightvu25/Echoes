using UnityEngine;

/// <summary>
/// Migrated Relic: 2x damage on final combo hit.
/// </summary>
public class IronRingRelic : MonoBehaviour, IRelicEffect
{
    private PlayerAttack playerAttack;
    private PlaystyleManager playstyleManager;

    public void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID)
    {
        playerAttack = GetComponent<PlayerAttack>();
        playstyleManager = GetComponent<PlaystyleManager>();
        eventBus.OnBeforeOutgoingDamage += HandleOutgoingDamage;
    }

    public void OnUnequip(PlayerEventBus eventBus)
    {
        eventBus.OnBeforeOutgoingDamage -= HandleOutgoingDamage;
    }

    private void HandleOutgoingDamage(IDamageable target, ref DamageInfo info)
    {
        if (playerAttack != null && playstyleManager != null)
        {
            var pData = playstyleManager.GetPlaystyleData(playerAttack.CurrentPlaystyle);
            if (pData != null && playerAttack.CurrentComboStep >= pData.comboSteps - 1)
            {
                info.multiplicativeStack *= 2.0f;
            }
        }
    }
}
