using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArcModifier : IEchoModifier
{
    public int Priority => 100;
    
    // Configurable variables for the arc chain
    public float chainRadius = 5f;
    public int maxBounces = 4;

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

        if (procSuccessful) status.ApplyInterrupt();

        var mb = ctx.PlayerGameObject.GetComponent<MonoBehaviour>();
        if (mb != null)
        {
            mb.StartCoroutine(ExecuteArcChainRoutine(e.target.Transform.position, e.damageInfo, e.target));
        }
    }

    private IEnumerator ExecuteArcChainRoutine(Vector2 startOrigin, DamageInfo originalInfo, IDamageable originalTarget)
    {
        LayerMask enemyLayer = LayerMask.GetMask("Enemy");
        
        List<IDamageable> alreadyHit = new List<IDamageable> { originalTarget };
        Vector2 currentOrigin = startOrigin;

        GameObject prefab = originalInfo.activeEcho != null ? originalInfo.activeEcho.chainLightningVFXPrefab : null;

        // Always draw the initial lightning bolt from the Player to the first target hit
        if (prefab != null)
        {
            GameObject vfxObj = Object.Instantiate(prefab);
            ChainLightningVFX vfx = vfxObj.GetComponent<ChainLightningVFX>();
            if (vfx != null) vfx.Initialize(ctx.PlayerGameObject.transform.position, startOrigin);
        }

        for (int i = 0; i < maxBounces; i++)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(currentOrigin, chainRadius, enemyLayer);
            IDamageable nearestTarget = null;
            float minDistance = float.MaxValue;
            Collider2D nearestCol = null;

            foreach (Collider2D col in colliders)
            {
                IDamageable target = col.GetComponentInParent<IDamageable>();
                if (target != null && !alreadyHit.Contains(target) && !target.IsDead)
                {
                    float dist = Vector2.Distance(currentOrigin, col.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestTarget = target;
                        nearestCol = col;
                    }
                }
            }

            if (nearestTarget == null) break;

            alreadyHit.Add(nearestTarget);

            // Delay for propagation
            yield return new WaitForSeconds(0.04f);

            // Camera Shake
            CinemachineCameraShake2D.Instance?.ShakeCamera(0.2f);

            // Spawn VFX
            if (prefab != null)
            {
                GameObject vfxObj = Object.Instantiate(prefab);
                ChainLightningVFX vfx = vfxObj.GetComponent<ChainLightningVFX>();
                if (vfx != null) vfx.Initialize(currentOrigin, nearestCol.transform.position);
            }

            // Deal Damage
            DamageInfo chainInfo = DamageInfo.Create(Mathf.RoundToInt(originalInfo.baseDamage * 0.5f), ctx.PlayerGameObject);
            chainInfo.damageSource = DamageSourceType.ArcChain;
            chainInfo.activeEcho = originalInfo.activeEcho;
            nearestTarget.TakeDamage(chainInfo);

            currentOrigin = nearestCol.transform.position;
        }
    }
}
