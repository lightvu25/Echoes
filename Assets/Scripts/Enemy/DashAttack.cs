using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;

    [SerializeField] private EnemyData data;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private LayerMask damageableLayers;

    public bool IsAttacking { get; private set; }

    private Rigidbody2D rb;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    private EnemySensor sensor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sensor = GetComponent<EnemySensor>();
    }

    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        IsAttacking = true;
        OnAttackStarted?.Invoke(this, EventArgs.Empty);
        StartCoroutine(DashRoutine());
    }

    public void CancelAttack()
    {
        IsAttacking = false;
        StopAllCoroutines();
        hitTargets.Clear();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator DashRoutine()
    {
        hitTargets.Clear();
        
        Vector2 dashDir = new Vector2(transform.lossyScale.x >= 0 ? 1f : -1f, 0f);
        if (sensor != null && sensor.TargetPlayer != null)
        {
            dashDir = ((Vector2)sensor.TargetPlayer.position - (Vector2)transform.position).normalized;
        }

        float timer = 0f;
        while (timer < dashDuration)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            CheckDashHits();
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        hitTargets.Clear();
        IsAttacking = false;
        OnAttackFinished?.Invoke(this, EventArgs.Empty);
    }

    private void CheckDashHits()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, new Vector2(1.2f, 1.2f), 0f, damageableLayers);

        foreach (var hit in hits)
        {
            if (hit.transform.root.gameObject == transform.root.gameObject) continue;

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDead) continue;
            if (hitTargets.Contains(target)) continue;
            hitTargets.Add(target);

            float dirX = target.Transform.position.x - transform.position.x;
            Vector2 knockbackDir = new Vector2(dirX >= 0 ? 1f : -1f, 0.3f).normalized;

            DamageInfo damageInfo = new DamageInfo
            {
                baseDamage = data != null ? data.attackBase : 10,
                flatBonus = 0,
                linearModifierSum = 0f,
                multiplicativeStack = 1f,
                procCoefficient = 1f,
                knockbackDirection = knockbackDir,
                knockbackForce = data != null ? data.knockbackForce * 1.5f : 5f,
                hitFreezeTime = 0f,
                attacker = gameObject,
                damageSource = DamageSourceType.DashAttack,
                isCritical = false
            };

            target.TakeDamage(damageInfo);
        }
    }
}
