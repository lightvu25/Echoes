using UnityEngine;

public class CryoStasisModifier : IEchoModifier
{
    public int Priority => 200; // Higher priority for fusion modifiers
    private EchoModifierContext ctx;

    public void Initialize(EchoModifierContext context)
    {
        ctx = context;
        if (ctx.PlayerAttackHitbox != null)
        {
            ctx.PlayerAttackHitbox.OnBeforeDamageApplied += HandleBeforeDamageApplied;
            ctx.PlayerAttackHitbox.OnHitTarget += HandleOnHitTarget;
        }
    }

    public void Remove()
    {
        if (ctx != null && ctx.PlayerAttackHitbox != null)
        {
            ctx.PlayerAttackHitbox.OnBeforeDamageApplied -= HandleBeforeDamageApplied;
            ctx.PlayerAttackHitbox.OnHitTarget -= HandleOnHitTarget;
        }
    }

    private void HandleBeforeDamageApplied(IDamageable target, ref DamageInfo damageInfo)
    {
        if (damageInfo.activeEcho == null || damageInfo.activeEcho.uniqueModifierID != "FUS_CRYO_STASIS") return;

        EchoStatusReceiver status = target.Transform.GetComponent<EchoStatusReceiver>();
        if (status == null) status = target.Transform.gameObject.AddComponent<EchoStatusReceiver>();

        damageInfo.knockbackForce = 0f;
        if (status.IsFrozen)
        {
            damageInfo.baseDamage = 99999;
            damageInfo.isTrueDamage = true;
        }
    }

    private void HandleOnHitTarget(object sender, AttackHitbox.HitEventArgs e)
    {
        if (e.damageInfo.activeEcho == null || e.damageInfo.activeEcho.uniqueModifierID != "FUS_CRYO_STASIS") return;

        EchoStatusReceiver status = e.target.Transform.GetComponent<EchoStatusReceiver>();
        if (status == null) status = e.target.Transform.gameObject.AddComponent<EchoStatusReceiver>();

        float baseChance = e.damageInfo.activeEcho.statusProcCoefficient;
        float currentProc = e.damageInfo.procCoefficient;
        bool procSuccessful = DamageCalculator.ShouldProc(baseChance, currentProc);

        if (procSuccessful) status.ApplyFreeze(4f);
    }
}
