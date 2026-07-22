using UnityEngine;

public class RagnarokModifier : IEchoModifier
{
    public int Priority => 200;
    private EchoModifierContext ctx;

    public void Initialize(EchoModifierContext context)
    {
        ctx = context;
        if (ctx.PlayerHealth != null)
        {
            ctx.PlayerHealth.OnBeforeTakeDamage += HandlePlayerDamageTaken;
        }
    }

    public void Remove()
    {
        if (ctx != null && ctx.PlayerHealth != null)
        {
            ctx.PlayerHealth.OnBeforeTakeDamage -= HandlePlayerDamageTaken;
        }
    }

    private void HandlePlayerDamageTaken(ref int damageAmount, ref DamageInfo info)
    {
        if (ctx.ActiveEchoData != null && ctx.ActiveEchoData.uniqueModifierID == "FUS_RAGNAROK")
        {
            if (damageAmount > 0)
            {
                damageAmount = 99999;
                info.isTrueDamage = true;
            }
        }
    }
}
