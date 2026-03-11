using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Timer-based attack hitbox. Multi-hit per swing allowed by design.
/// </summary>
public class AttackHitbox : MonoBehaviour
{
    public event EventHandler<HitEventArgs> OnHitTarget;

    public class HitEventArgs : EventArgs
    {
        public IDamageable target;
        public DamageInfo damageInfo;
        public int finalDamage;
    }

    [Header("Hitbox Settings")]
    [SerializeField] private Vector2 hitboxSize = new Vector2(1f, 1f);
    [SerializeField] private Vector2 hitboxOffset = new Vector2(0.5f, 0f);
    [SerializeField] private LayerMask targetLayers;

    [Header("Timing")]
    [SerializeField] private float startupTime = 0.05f;
    [SerializeField] private float activeTime = 0.15f;
    [Tooltip("Interval between multi-hits (0 = single hit per target)")]
    [SerializeField] private float multiHitInterval = 0f;

    [Header("Damage")]
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float procCoefficient = 1f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float hitFreezeTime = 0.05f;

    [SerializeField] private CombatStats combatStats;

    private bool isActive = false;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private Dictionary<IDamageable, float> multiHitTimers = new Dictionary<IDamageable, float>();
    private GameObject owner;

    private void Awake()
    {
        owner = transform.root.gameObject;
    }

    public void Activate()
    {
        if (isActive) return;
        StartCoroutine(HitboxRoutine());
    }

    public void Activate(int damage, float procCoef = 1f)
    {
        baseDamage = damage;
        procCoefficient = procCoef;
        Activate();
    }

    public void Deactivate()
    {
        isActive = false;
        StopAllCoroutines();
        hitTargets.Clear();
        multiHitTimers.Clear();
    }

    private IEnumerator HitboxRoutine()
    {
        if (startupTime > 0f) yield return new WaitForSeconds(startupTime);

        isActive = true;
        hitTargets.Clear();
        multiHitTimers.Clear();

        float timer = 0f;
        while (timer < activeTime)
        {
            CheckHits();
            timer += Time.deltaTime;
            yield return null;
        }

        isActive = false;
        hitTargets.Clear();
        multiHitTimers.Clear();
    }

    private void CheckHits()
    {
        if (!isActive) return;

        Vector2 direction = owner.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * direction.x, hitboxOffset.y);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f, targetLayers);

        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDead) continue;

            if (multiHitInterval > 0f)
            {
                if (multiHitTimers.TryGetValue(target, out float lastHit) && Time.time - lastHit < multiHitInterval) continue;
                multiHitTimers[target] = Time.time;
            }
            else
            {
                if (hitTargets.Contains(target)) continue;
                hitTargets.Add(target);
            }

            ApplyDamage(target);
        }
    }

    private void ApplyDamage(IDamageable target)
    {
        Vector2 knockbackDir = (target.Transform.position - transform.position).normalized;
        if (knockbackDir == Vector2.zero)
            knockbackDir = owner.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

        DamageInfo damageInfo = new DamageInfo
        {
            baseDamage = baseDamage,
            flatBonus = 0,
            linearModifierSum = 0f,
            multiplicativeStack = 1f,
            procCoefficient = procCoefficient,
            knockbackDirection = knockbackDir,
            knockbackForce = knockbackForce,
            hitFreezeTime = hitFreezeTime,
            attacker = owner,
            damageSource = "Attack",
            isCritical = false
        };

        if (combatStats != null && UnityEngine.Random.value < combatStats.critChance)
        {
            damageInfo.isCritical = true;
            damageInfo.multiplicativeStack *= combatStats.critMultiplier;
        }

        int finalDamage = DamageCalculator.CalculateFinalDamage(damageInfo, target.Defense);
        target.TakeDamage(damageInfo);

        OnHitTarget?.Invoke(this, new HitEventArgs { target = target, damageInfo = damageInfo, finalDamage = finalDamage });
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * direction.x, hitboxOffset.y);
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(center, hitboxSize);
    }
}
