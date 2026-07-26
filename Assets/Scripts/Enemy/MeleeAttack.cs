using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;

    [SerializeField] private EnemyData data;
    [SerializeField] private float activeTime = 0.1f;

    public bool IsAttacking { get; private set; }

    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private bool isHitboxActive;
    private const float FRIENDLY_FIRE_MULTIPLIER = 0.5f;

    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        IsAttacking = true;
        OnAttackStarted?.Invoke(this, EventArgs.Empty);
        
        StartCoroutine(SafetyTimeout());
    }

    private IEnumerator SafetyTimeout()
    {
        yield return new WaitForSeconds(3f);
        if (IsAttacking) FinishAttack();
    }

    public void CancelAttack()
    {
        isHitboxActive = false;
        IsAttacking = false;
        StopAllCoroutines();
        hitTargets.Clear();
    }

    public void TriggerHitbox()
    {
        if (isHitboxActive) return;
        StartCoroutine(HitboxRoutine());
    }

    private IEnumerator HitboxRoutine()
    {
        isHitboxActive = true;
        hitTargets.Clear();

        float timer = 0f;
        while (timer < activeTime)
        {
            CheckHits();
            timer += Time.deltaTime;
            yield return null;
        }

        isHitboxActive = false;
        hitTargets.Clear();
    }

    public void FinishAttack()
    {
        if (!IsAttacking) return;
        IsAttacking = false;
        OnAttackFinished?.Invoke(this, EventArgs.Empty);
    }

    private void CheckHits()
    {
        if (!isHitboxActive || data == null) return;

        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 offset = new Vector2(data.attackHitboxOffset.x * direction.x, data.attackHitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, data.attackHitboxSize, 0f, data.attackTargetLayers);

        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDead) continue;
            if (target.Transform == this.transform) continue;

            if (hitTargets.Contains(target)) continue;
            hitTargets.Add(target);

            ApplyDamage(target, hit.gameObject);
        }
    }

    private void ApplyDamage(IDamageable target, GameObject targetGO)
    {
        float dirX = target.Transform.position.x - transform.position.x;
        Vector2 knockbackDir = new Vector2(dirX >= 0 ? 1f : -1f, 0f);
        bool isFriendlyFire = targetGO.GetComponent<EnemyCombat>() != null;

        DamageInfo damageInfo = new DamageInfo
        {
            baseDamage = data.attackBase,
            flatBonus = 0,
            linearModifierSum = 0f,
            multiplicativeStack = isFriendlyFire ? FRIENDLY_FIRE_MULTIPLIER : 1f,
            procCoefficient = 1f,
            knockbackDirection = knockbackDir,
            knockbackForce = data.knockbackForce,
            hitFreezeTime = 0f,
            attacker = gameObject,
            damageSource = DamageSourceType.MeleeAttack,
            isCritical = false
        };

        target.TakeDamage(damageInfo);
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 offset = new Vector2(data.attackHitboxOffset.x * direction.x, data.attackHitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;
        Gizmos.color = isHitboxActive ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(center, data.attackHitboxSize);
    }
}