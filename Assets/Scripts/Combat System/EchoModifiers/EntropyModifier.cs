using UnityEngine;

public class EntropyModifier : IEchoModifier
{
    public int Priority => 200;
    private EchoModifierContext ctx;

    public void Initialize(EchoModifierContext context)
    {
        ctx = context;
        if (ctx.PlayerAttackHitbox != null)
        {
            ctx.PlayerAttackHitbox.OnBeforeDamageApplied += HandleBeforeDamageApplied;
        }
    }

    public void Remove()
    {
        if (ctx != null && ctx.PlayerAttackHitbox != null)
        {
            ctx.PlayerAttackHitbox.OnBeforeDamageApplied -= HandleBeforeDamageApplied;
        }
    }

    private void HandleBeforeDamageApplied(IDamageable target, ref DamageInfo damageInfo)
    {
        if (damageInfo.activeEcho == null || damageInfo.activeEcho.uniqueModifierID != "FUS_ENTROPY") return;

        damageInfo.isTrueDamage = true;
        // Randomize damage multiplier instead of scaling the player's transform
        float randMultiplier = UnityEngine.Random.Range(0.5f, 2.0f);
        damageInfo.multiplicativeStack *= randMultiplier;
    }
}
