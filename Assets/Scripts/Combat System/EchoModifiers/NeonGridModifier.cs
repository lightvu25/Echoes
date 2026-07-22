using UnityEngine;

public class NeonGridModifier : IEchoModifier
{
    public int Priority => 200;
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
        if (e.damageInfo.activeEcho == null || e.damageInfo.activeEcho.uniqueModifierID != "FUS_NEON_GRID") return;

        if (ctx.GlitchedZonePrefab != null)
        {
            Object.Instantiate(ctx.GlitchedZonePrefab, e.target.Transform.position, Quaternion.identity);
        }
    }
}
