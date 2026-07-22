using UnityEngine;

public class CurseModifier : IEchoModifier
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
        if (e.damageInfo.activeEcho == null || e.damageInfo.activeEcho.uniqueModifierID != "OBLIVION") return;

        int selfDamage = Mathf.Max(1, Mathf.RoundToInt(ctx.PlayerHealth.MaxHP * 0.02f));
        DamageInfo curseDamage = DamageInfo.Create(selfDamage, ctx.PlayerGameObject);
        curseDamage.isTrueDamage = true;
        curseDamage.damageSource = "OblivionSelfDamage";
        ctx.PlayerHealth.TakeDamage(curseDamage);
    }
}
