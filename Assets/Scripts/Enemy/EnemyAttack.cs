using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles enemy attack hitbox using frame-perfect, code-based overlap checks.
/// Supports both Animation Event triggers and auto-trigger from EnemyInteract.
/// Detects both Player and Enemy layers for friendly fire.
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    public event EventHandler<HitEventArgs> OnHitTarget;

    public class HitEventArgs : EventArgs
    {
        public IDamageable target;
        public DamageInfo damageInfo;
        public int finalDamage;
    }

    [Header("References")]
    [SerializeField] private EnemyData data;

    [Header("Timing")]
    [Tooltip("Startup delay before hitbox activates (auto-trigger mode)")]
    [SerializeField] private float startupDelay = 0.2f;
    [Tooltip("Duration hitbox stays active after trigger")]
    [SerializeField] private float activeTime = 0.1f;

    // State
    private bool isActive = false;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private EnemyInteract enemyInteract;

    /// <summary>
    /// Friendly fire damage multiplier (50% reduction).
    /// </summary>
    private const float FRIENDLY_FIRE_MULTIPLIER = 0.5f;

    private void Awake()
    {
        enemyInteract = GetComponent<EnemyInteract>();
    }

    private void Start()
    {
        // Auto-trigger: subscribe to EnemyInteract attack event
        if (enemyInteract != null)
        {
            enemyInteract.OnAttack += EnemyInteract_OnAttack;
        }
    }

    private void OnDestroy()
    {
        if (enemyInteract != null)
        {
            enemyInteract.OnAttack -= EnemyInteract_OnAttack;
        }
    }

    private void EnemyInteract_OnAttack(object sender, EventArgs e)
    {
        StartCoroutine(AutoAttackRoutine());
    }

    private IEnumerator AutoAttackRoutine()
    {
        // Startup delay (anticipation frames)
        if (startupDelay > 0f)
        {
            yield return new WaitForSeconds(startupDelay);
        }

        TriggerHitbox();
    }

    /// <summary>
    /// Activate the attack hitbox. Wire this to an Animation Event
    /// on the exact attack frame for frame-perfect timing.
    /// </summary>
    public void TriggerHitbox()
    {
        if (isActive) return;
        StartCoroutine(HitboxRoutine());
    }

    /// <summary>
    /// Force cancel the hitbox.
    /// </summary>
    public void CancelHitbox()
    {
        isActive = false;
        StopAllCoroutines();
        hitTargets.Clear();
    }

    private IEnumerator HitboxRoutine()
    {
        isActive = true;
        hitTargets.Clear();

        float timer = 0f;
        while (timer < activeTime)
        {
            CheckHits();
            timer += Time.deltaTime;
            yield return null;
        }

        isActive = false;
        hitTargets.Clear();
    }

    private void CheckHits()
    {
        if (!isActive || data == null) return;

        // Calculate hitbox position based on facing direction
        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 offset = new Vector2(data.attackHitboxOffset.x * direction.x, data.attackHitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;

        // Find all targets in hitbox (Player + Enemy layers)
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, data.attackHitboxSize, 0f, data.attackTargetLayers);

        foreach (var hit in hits)
        {
            // Skip self — attacker cannot hurt itself
            if (hit.gameObject == gameObject) continue;

            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null || target.IsDead) continue;

            // Single hit per target per activation
            if (hitTargets.Contains(target)) continue;
            hitTargets.Add(target);

            ApplyDamage(target, hit.gameObject);
        }
    }

    private void ApplyDamage(IDamageable target, GameObject targetGO)
    {
        // Horizontal-only knockback direction (away from attacker)
        float dirX = target.Transform.position.x - transform.position.x;
        Vector2 knockbackDir = new Vector2(dirX >= 0 ? 1f : -1f, 0f);

        // Check if friendly fire (enemy hitting another enemy)
        bool isFriendlyFire = targetGO.GetComponent<EnemyCombat>() != null;

        // Build damage info
        DamageInfo damageInfo = new DamageInfo
        {
            baseDamage = data.attackBase,
            flatBonus = 0,
            linearModifierSum = 0f,
            multiplicativeStack = isFriendlyFire ? FRIENDLY_FIRE_MULTIPLIER : 1f,
            procCoefficient = 1f,
            knockbackDirection = knockbackDir,
            knockbackForce = data.knockbackForce,
            hitFreezeTime = 0f, // No hit freeze for enemy attacks
            attacker = gameObject,
            damageSource = "EnemyAttack",
            isCritical = false // Crits are player-only
        };

        // Calculate final damage for event
        int finalDamage = DamageCalculator.CalculateFinalDamage(damageInfo, target.Defense);

        // Apply damage
        target.TakeDamage(damageInfo);

        // Fire event
        OnHitTarget?.Invoke(this, new HitEventArgs
        {
            target = target,
            damageInfo = damageInfo,
            finalDamage = finalDamage
        });
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 offset = new Vector2(data.attackHitboxOffset.x * direction.x, data.attackHitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;

        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(center, data.attackHitboxSize);
    }
}
