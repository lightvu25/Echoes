using UnityEngine;

public class AnomalyModifier : IEchoModifier
{
    public int Priority => 100;
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
        if (damageInfo.activeEcho == null || damageInfo.activeEcho.uniqueModifierID != "DISTORTION") return;

        if (true /*UnityEngine.Random.value <= 0.25f*/)
        {
            damageInfo.isTrueDamage = true;
        }
    }
}
