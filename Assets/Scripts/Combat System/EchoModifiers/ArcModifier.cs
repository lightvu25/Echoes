using UnityEngine;

public class ArcModifier : IEchoModifier
{
    public int Priority => 100;
    private EchoModifierContext ctx;

    public void Initialize(EchoModifierContext context)
    {
        ctx = context;
        if (ctx.PlayerAttackHitbox != null)
        {
            ctx.PlayerAttackHitbox.OnHitTarget += HandleOnHitTarget;
        }
    }

    public void Remove()
    {
        if (ctx != null && ctx.PlayerAttackHitbox != null)
        {
            ctx.PlayerAttackHitbox.OnHitTarget -= HandleOnHitTarget;
        }
    }

    private void HandleOnHitTarget(object sender, AttackHitbox.HitEventArgs e)
    {
        if (e.damageInfo.activeEcho == null || e.damageInfo.activeEcho.uniqueModifierID != "CHAIN_ARC") return;
        if (e.damageInfo.damageSource == DamageSourceType.ArcChain || e.damageInfo.damageSource == DamageSourceType.BlackHole) return;

        EchoStatusReceiver status = e.target.Transform.GetComponent<EchoStatusReceiver>();
        if (status == null) status = e.target.Transform.gameObject.AddComponent<EchoStatusReceiver>();

        float baseChance = e.damageInfo.activeEcho.statusProcCoefficient;
        float currentProc = e.damageInfo.procCoefficient;
        bool procSuccessful = DamageCalculator.ShouldProc(baseChance, currentProc);

        ExecuteArcChain(e.target.Transform.position, e.damageInfo, e.target);
        if (procSuccessful) status.ApplyInterrupt();
    }

    private void ExecuteArcChain(Vector2 origin, DamageInfo originalInfo, IDamageable originalTarget)
    {
        float chainRadius = 5f;
        int maxBounces = 2;
        LayerMask enemyLayer = LayerMask.GetMask("Enemy");

        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, chainRadius, enemyLayer);
        int hitCount = 0;

        foreach (Collider2D col in colliders)
        {
            if (hitCount >= maxBounces) break;

            IDamageable nextTarget = col.GetComponent<IDamageable>();
            if (nextTarget != null && nextTarget.Transform != originalTarget.Transform) 
            {
                DamageInfo chainInfo = DamageInfo.Create(Mathf.RoundToInt(originalInfo.baseDamage * 0.5f), ctx.PlayerGameObject);
                chainInfo.damageSource = DamageSourceType.ArcChain;
                chainInfo.activeEcho = originalInfo.activeEcho;

                nextTarget.TakeDamage(chainInfo);
                
                hitCount++;
            }
        }
    }
}
