using UnityEngine;

public class BlazeModifier : IEchoModifier
{
    public int Priority => 100;
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
        if (damageInfo.activeEcho == null || damageInfo.activeEcho.uniqueModifierID != "IGNITION") return;

        HealthSystem targetHealth = target.Transform.GetComponent<HealthSystem>();
        if (targetHealth != null)
        {
            float missingHpPercent = 1f - ((float)targetHealth.CurrentHP / targetHealth.MaxHP);
            damageInfo.multiplicativeStack *= (1f + (0.10f * missingHpPercent)); 
        }
    }

    private void HandleOnHitTarget(object sender, AttackHitbox.HitEventArgs e)
    {
        if (e.damageInfo.activeEcho == null || e.damageInfo.activeEcho.uniqueModifierID != "IGNITION") return;
        if (e.damageInfo.damageSource == DamageSourceType.ArcChain || e.damageInfo.damageSource == DamageSourceType.BlackHole) return;

        EchoStatusReceiver status = e.target.Transform.GetComponent<EchoStatusReceiver>();
        if (status == null) status = e.target.Transform.gameObject.AddComponent<EchoStatusReceiver>();

        float baseChance = e.damageInfo.activeEcho.statusProcCoefficient;
        float currentProc = e.damageInfo.procCoefficient;
        bool procSuccessful = DamageCalculator.ShouldProc(baseChance, currentProc);

        if (procSuccessful) status.ApplyBurn(3f);
    }
}
