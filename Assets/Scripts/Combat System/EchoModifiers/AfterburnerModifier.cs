using UnityEngine;

public class AfterburnerModifier : IEchoModifier
{
    public int Priority => 200;
    private EchoModifierContext ctx;
    private PlayerAttack playerAttack;

    public void Initialize(EchoModifierContext context)
    {
        ctx = context;
        if (ctx.PlayerGameObject != null)
        {
            playerAttack = ctx.PlayerGameObject.GetComponent<PlayerAttack>();
            if (playerAttack != null)
            {
                playerAttack.OnAttackStarted += HandleAttackStarted;
            }
        }
    }

    public void Remove()
    {
        if (playerAttack != null)
        {
            playerAttack.OnAttackStarted -= HandleAttackStarted;
        }
    }

    private void HandleAttackStarted(object sender, PlayerAttack.AttackEventArgs e)
    {
        if (ctx.ActiveEchoData != null && ctx.ActiveEchoData.uniqueModifierID == "FUS_AFTERBURNER")
        {
            if (ctx.FireTrailPrefab != null)
            {
                 Vector3 pos = ctx.PlayerGameObject.transform.position;
                 Object.Instantiate(ctx.FireTrailPrefab, pos, Quaternion.identity);
            }
        }
    }
}
