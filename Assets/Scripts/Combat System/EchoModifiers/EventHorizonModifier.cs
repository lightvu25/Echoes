using UnityEngine;

public class EventHorizonModifier : IEchoModifier
{
    public int Priority => 200;
    private EchoModifierContext ctx;
    private float eventHorizonCooldown = 10f;
    private float eventHorizonTimer = 0f;

    public void Initialize(EchoModifierContext context)
    {
        ctx = context;
        if (ctx.PlayerAttackHitbox != null)
        {
            ctx.PlayerAttackHitbox.OnHitTarget += HandleOnHitTarget;
        }
        eventHorizonTimer = Time.time - eventHorizonCooldown; // Ready immediately
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
        if (e.damageInfo.activeEcho == null || e.damageInfo.activeEcho.uniqueModifierID != "FUS_EVENT_HORIZON") return;
        if (e.damageInfo.damageSource == "ArcChain" || e.damageInfo.damageSource == "BlackHole") return;

        if (Time.time >= eventHorizonTimer + eventHorizonCooldown)
        {
            if (ctx.EventHorizonPrefab != null)
            {
                GameObject bh = Object.Instantiate(ctx.EventHorizonPrefab, e.target.Transform.position, Quaternion.identity);
                BlackHoleEffect bhEffect = bh.GetComponent<BlackHoleEffect>();
                if (bhEffect != null) bhEffect.Initialize(ctx.PlayerGameObject);
            }
            eventHorizonTimer = Time.time;
        }
    }
}
