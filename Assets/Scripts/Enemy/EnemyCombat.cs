using System;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class EnemyCombat : MonoBehaviour, IDamageable
{
    public event EventHandler<DamageReceivedArgs> OnDamageReceived;
    public event EventHandler OnEnemyDied;

    public class DamageReceivedArgs : EventArgs
    {
        public int damage;
        public Vector2 knockbackDir;
    }

    [Header("References")]
    [SerializeField] private EnemyData data;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;

    private HealthSystem healthSystem;
    private EnemyMovement enemyMovement;
    private Rigidbody2D rb;
    private bool isKnockedBack = false;

    public bool IsDead => healthSystem != null && healthSystem.IsDead;
    public Transform Transform => transform;
    public float Defense => healthSystem != null ? healthSystem.Defense : 0f;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        enemyMovement = GetComponent<EnemyMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Initialize from enemy data
        if (data != null && healthSystem != null)
        {
            healthSystem.SetMaxHP(data.maxHP, true);
            healthSystem.SetDefense(data.defense);
        }

        if (healthSystem != null)
        {
            healthSystem.OnDeath += HealthSystem_OnDeath;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= HealthSystem_OnDeath;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (healthSystem == null || healthSystem.IsDead) return;
        if (healthSystem.IsInvincible) return;

        // Apply damage through health system
        healthSystem.TakeDamage(damageInfo);

        // Apply knockback
        if (damageInfo.knockbackForce > 0f && rb != null)
        {
            ApplyKnockback(damageInfo.knockbackDirection, damageInfo.knockbackForce);
        }

        int finalDamage = DamageCalculator.CalculateFinalDamage(damageInfo, Defense);
        OnDamageReceived?.Invoke(this, new DamageReceivedArgs
        {
            damage = finalDamage,
            knockbackDir = damageInfo.knockbackDirection
        });
    }

    private void ApplyKnockback(Vector2 direction, float force)
    {
        if (isKnockedBack) return;

        StartCoroutine(KnockbackRoutine(direction, force));
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        isKnockedBack = true;

        if (enemyMovement != null)
            enemyMovement.SetKnockedBack(true);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;

        if (enemyMovement != null)
            enemyMovement.SetKnockedBack(false);
    }

    private void HealthSystem_OnDeath(object sender, EventArgs e)
    {
        OnEnemyDied?.Invoke(this, EventArgs.Empty);
    }

    public int CurrentHP => healthSystem != null ? healthSystem.CurrentHP : 0;
    public int MaxHP => healthSystem != null ? healthSystem.MaxHP : 0;
    public float HPPercent => healthSystem != null ? healthSystem.HPPercent : 0f;
    public bool IsKnockedBack => isKnockedBack;
}
