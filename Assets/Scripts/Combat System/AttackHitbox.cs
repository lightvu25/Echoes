using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Timer-based attack hitbox that can hit multiple targets.
/// Multi-hit per swing is allowed as per design.
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
    [Tooltip("Delay before hitbox becomes active")]
    [SerializeField] private float startupTime = 0.05f;
    [Tooltip("Duration hitbox stays active")]
    [SerializeField] private float activeTime = 0.15f;
    [Tooltip("Interval between multi-hits (0 = single hit per target)")]
    [SerializeField] private float multiHitInterval = 0f;

    [Header("Damage")]
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float procCoefficient = 1f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float hitFreezeTime = 0.05f;

    // State
    private bool isActive = false;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private Dictionary<IDamageable, float> multiHitTimers = new Dictionary<IDamageable, float>();
    private GameObject owner;
    private CombatStats combatStats;

    private void Awake()
    {
        owner = transform.root.gameObject;
        combatStats = owner.GetComponent<CombatStats>();
    }

    /// <summary>
    /// Activate the hitbox with timer-based activation.
    /// </summary>
    public void Activate()
    {
        if (isActive) return;
        StartCoroutine(HitboxRoutine());
    }

    /// <summary>
    /// Activate with custom damage info.
    /// </summary>
    public void Activate(int damage, float procCoef = 1f)
    {
        baseDamage = damage;
        procCoefficient = procCoef;
        Activate();
    }

    /// <summary>
    /// Force deactivate the hitbox.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        StopAllCoroutines();
        hitTargets.Clear();
        multiHitTimers.Clear();
    }

    private IEnumerator HitboxRoutine()
    {
        // Startup delay
        if (startupTime > 0f)
        {
            yield return new WaitForSeconds(startupTime);
        }

        // Active phase
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

        // End
        isActive = false;
        hitTargets.Clear();
        multiHitTimers.Clear();
    }

    private void CheckHits()
    {
        if (!isActive) return;

        // Calculate hitbox position based on facing direction
        Vector2 direction = owner.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 offset = new Vector2(hitboxOffset.x * direction.x, hitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;

        // Find all targets in hitbox
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f, targetLayers);

        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null) continue;
            if (target.IsDead) continue;

            // Multi-hit logic
            if (multiHitInterval > 0f)
            {
                // Allow multi-hits with interval
                if (multiHitTimers.TryGetValue(target, out float lastHitTime))
                {
                    if (Time.time - lastHitTime < multiHitInterval)
                        continue;
                }
                multiHitTimers[target] = Time.time;
            }
            else
            {
                // Single hit per activation (but still allows hitting multiple targets)
                if (hitTargets.Contains(target))
                    continue;
                hitTargets.Add(target);
            }

            // Apply damage
            ApplyDamage(target);
        }
    }

    private void ApplyDamage(IDamageable target)
    {
        // Calculate knockback direction
        Vector2 knockbackDir = (target.Transform.position - transform.position).normalized;
        if (knockbackDir == Vector2.zero)
        {
            knockbackDir = owner.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        }

        // Create damage info
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

        // Apply crit if we have stats
        if (combatStats != null && Random.value < combatStats.critChance)
        {
            damageInfo.isCritical = true;
            damageInfo.multiplicativeStack *= combatStats.critMultiplier;
        }

        // Calculate final damage for event
        int finalDamage = DamageCalculator.CalculateFinalDamage(damageInfo, target.Defense);

        // Apply damage
        target.TakeDamage(damageInfo);

        // Fire event for item procs
        OnHitTarget?.Invoke(this, new HitEventArgs
        {
            target = target,
            damageInfo = damageInfo,
            finalDamage = finalDamage
        });
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 offset = new Vector2(hitboxOffset.x * direction.x, hitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;

        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(center, hitboxSize);
    }
}
