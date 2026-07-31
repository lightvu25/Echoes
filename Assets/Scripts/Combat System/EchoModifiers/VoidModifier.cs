using UnityEngine;
using System.Collections.Generic;

public class VoidModifier : IEchoModifier
{
    public int Priority => 100;
    private EchoModifierContext ctx;
    private Dictionary<IDamageable, GameObject> activeMarks = new Dictionary<IDamageable, GameObject>();

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
        
        // Clean up marks
        List<IDamageable> targets = new List<IDamageable>(activeMarks.Keys);
        foreach (var target in targets)
        {
            RemoveMark(target);
        }
        activeMarks.Clear();
    }

    private void HandleTargetDeath(object sender, System.EventArgs e)
    {
        HealthSystem hs = sender as HealthSystem;
        if (hs != null)
        {
            IDamageable target = hs.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                RemoveMark(target);
            }
        }
    }

    private void RemoveMark(IDamageable target)
    {
        if (activeMarks.TryGetValue(target, out GameObject markVFX))
        {
            if (markVFX != null)
            {
                ObjectPoolManager.ReturnObjectToPool(markVFX);
            }
            activeMarks.Remove(target);
        }
        
        if (target != null && target.Transform != null)
        {
            EchoStatusReceiver status = target.Transform.GetComponent<EchoStatusReceiver>();
            if (status != null) status.IsVoidMarked = false;
            
            HealthSystem hs = target.Transform.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.OnDeath -= HandleTargetDeath;
            }
        }
    }

    private void HandleOnHitTarget(object sender, AttackHitbox.HitEventArgs e)
    {
        if (e.damageInfo.activeEcho == null || e.damageInfo.activeEcho.uniqueModifierID != "VOID_MARK") return;
        if (e.damageInfo.damageSource == DamageSourceType.VoidDetonate) return;
        if (e.target == null || e.target.IsDead) return;

        EchoStatusReceiver status = e.target.Transform.GetComponent<EchoStatusReceiver>();
        if (status == null) status = e.target.Transform.gameObject.AddComponent<EchoStatusReceiver>();

        if (!status.IsVoidMarked)
        {
            status.IsVoidMarked = true;
            if (e.damageInfo.activeEcho.voidMarkVFXPrefab != null)
            {
                GameObject mark = ObjectPoolManager.SpawnObject(e.damageInfo.activeEcho.voidMarkVFXPrefab, e.target.Transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
                
                VFXFollower follower = mark.GetComponent<VFXFollower>();
                if (follower == null) follower = mark.AddComponent<VFXFollower>();
                follower.Initialize(e.target.Transform, Vector3.up * 1.5f);

                activeMarks[e.target] = mark;

                HealthSystem hs = e.target.Transform.GetComponent<HealthSystem>();
                if (hs != null)
                {
                    // Unsubscribe just in case, then subscribe to prevent duplicate subscriptions
                    hs.OnDeath -= HandleTargetDeath;
                    hs.OnDeath += HandleTargetDeath;
                }
            }
        }
        else
        {
            RemoveMark(e.target);

            int baseAttack = e.damageInfo.baseDamage;
            int detonateDamage = Mathf.RoundToInt(baseAttack * 1.2f);
            DamageInfo detonation = DamageInfo.Create(detonateDamage, ctx.PlayerGameObject);
            detonation.isTrueDamage = true;
            detonation.damageSource = DamageSourceType.VoidDetonate;
            
            e.target.TakeDamage(detonation);
        }
    }
}
