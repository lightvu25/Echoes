using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;
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

    public bool IsAttacking { get; private set; }

    private const float FRIENDLY_FIRE_MULTIPLIER = 0.5f;

    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        IsAttacking = true;
        OnAttackStarted?.Invoke(this, EventArgs.Empty);
    }

    public void CancelAttack()
    {
        IsAttacking = false;
        CancelHitbox();
    }

    public void FinishAttackFromAnimation()
    {
        if (!IsAttacking) return;
        IsAttacking = false;
        OnAttackFinished?.Invoke(this, EventArgs.Empty);
    }

    public void TriggerHitbox()
    {
        if (isActive) return;
        StartCoroutine(HitboxRoutine());
    }

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
            // Skip self
            if (hit.transform.root.gameObject == this.transform.root.gameObject) continue;

            IDamageable target = hit.GetComponentInParent<IDamageable>();
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
        bool isFriendlyFire = targetGO.GetComponent<EnemyBrain>() != null;

        float damageMult = BurdenManager.Instance != null ? BurdenManager.Instance.CurrentDamageMultiplier : 1f;
        int scaledDamage = Mathf.RoundToInt(data.attackBase * damageMult);

        // Build damage info
        DamageInfo damageInfo = new DamageInfo
        {
            baseDamage = scaledDamage,
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