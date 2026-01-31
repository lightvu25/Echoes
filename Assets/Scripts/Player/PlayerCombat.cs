using System;
using UnityEngine;

/// <summary>
/// Main player combat controller.
/// Handles damage receiving, health, and integrates with combat systems.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class PlayerCombat : MonoBehaviour, IDamageable
{
    public event EventHandler<DamageReceivedArgs> OnDamageReceived;

    public class DamageReceivedArgs : EventArgs
    {
        public int damage;
        public Vector2 knockbackDir;
    }

    [Header("References")]
    [SerializeField] private CombatStats combatStats;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;

    private HealthSystem healthSystem;
    private Rigidbody2D rb;
    private bool isKnockedBack = false;

    // IDamageable implementation
    public bool IsDead => healthSystem != null && healthSystem.IsDead;
    public Transform Transform => transform;
    public float Defense => healthSystem != null ? healthSystem.Defense : 0f;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Initialize from combat stats
        if (combatStats != null && healthSystem != null)
        {
            healthSystem.SetMaxHP(combatStats.maxHP, true);
            healthSystem.SetDefense(combatStats.defense);
        }

        // Subscribe to health events
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

    /// <summary>
    /// IDamageable implementation - receive damage.
    /// </summary>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (healthSystem == null || healthSystem.IsDead) return;

        // Apply damage through health system
        healthSystem.TakeDamage(damageInfo);

        // Apply knockback
        if (damageInfo.knockbackForce > 0f && rb != null)
        {
            ApplyKnockback(damageInfo.knockbackDirection, damageInfo.knockbackForce);
        }

        // Fire event
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

        // Apply knockback force
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;
    }

    private void HealthSystem_OnDeath(object sender, EventArgs e)
    {
        // Trigger player death through PlayerInteract
        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.Dead();
        }
    }

    /// <summary>
    /// Get current HP.
    /// </summary>
    public int CurrentHP => healthSystem != null ? healthSystem.CurrentHP : 0;

    /// <summary>
    /// Get max HP.
    /// </summary>
    public int MaxHP => healthSystem != null ? healthSystem.MaxHP : 0;

    /// <summary>
    /// Get HP percentage (0-1).
    /// </summary>
    public float HPPercent => healthSystem != null ? healthSystem.HPPercent : 0f;

    /// <summary>
    /// Check if currently knocked back.
    /// </summary>
    public bool IsKnockedBack => isKnockedBack;
}
