using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RelicBurnZone : MonoBehaviour
{
    private readonly List<Collider2D> hits = new List<Collider2D>(32);
    private readonly HashSet<IDamageable> unique = new HashSet<IDamageable>();
    private GameObject owner;
    private float expiresAt;
    private float radius;
    private int tickDamage;
    private float nextTick;
    private ContactFilter2D filter;
    private Action<RelicBurnZone> expiredCallback;

    public void Initialize(GameObject source, float duration, float effectRadius, int damage,
        Action<RelicBurnZone> onExpired = null)
    {
        owner = source;
        expiresAt = Time.time + duration;
        radius = effectRadius;
        tickDamage = damage;
        nextTick = Time.time;
        expiredCallback = onExpired;
        filter = new ContactFilter2D();
        filter.SetLayerMask(RelicRuntimeContext.EnemyMask);
        filter.useTriggers = true;
    }

    private void Update()
    {
        if (Time.time >= expiresAt) { Destroy(gameObject); return; }
        if (Time.time < nextTick) return;
        nextTick = Time.time + 0.5f;

        hits.Clear();
        unique.Clear();
        Physics2D.OverlapCircle(transform.position, radius, filter, hits);
        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (!RelicRuntimeContext.IsValid(target) || target.IsDead || !unique.Add(target)) continue;
            DamageInfo damage = DamageInfo.Create(tickDamage, owner);
            damage.damageSource = DamageSourceType.RelicArea;
            target.TakeDamage(damage);
            target.Transform.GetComponent<EchoStatusReceiver>()?.ApplyBurn(1f, null, owner);
        }
    }

    private void OnDestroy()
    {
        expiredCallback?.Invoke(this);
        expiredCallback = null;
    }
}
