using System;
using System.Collections;
using UnityEngine;

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
    private PlayerAttack playerAttack;
    private Rigidbody2D rb;
    private Coroutine _knockbackCoroutine;
    public bool isKnockedBack = false;

    public bool IsDead => healthSystem != null && healthSystem.IsDead;
    public Transform Transform => transform;
    public float Defense => healthSystem != null ? healthSystem.Defense : 0f;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        playerAttack = GetComponent<PlayerAttack>();
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

        // Skip damage during dash i-frames or regular i-frames
        if (healthSystem.IsInvincible) return;

        // Apply damage through health system
        healthSystem.TakeDamage(damageInfo);

        // Apply knockback
        if (damageInfo.knockbackForce > 0f && rb != null)
        {
            ApplyKnockback(damageInfo.knockbackDirection, damageInfo.knockbackForce);
        }

        // Camera shake on player hit (no hit-stop for enemy attacks)
        if (CameraShaker.Instance != null)
            CameraShaker.Instance.BasicShake(3f, 0.3f);

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
        // If already knocked back, stop the previous coroutine before starting a new one
        // so overlapping hits don't leave isKnockedBack permanently true.
        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            isKnockedBack = false;
        }

        // Cancel any active attack — knockback interrupts player actions
        if (playerAttack != null)
            playerAttack.CancelAttack();

        _knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction, force));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        isKnockedBack = true;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;
    }

    private void HealthSystem_OnDeath(object sender, EventArgs e)
    {
        isKnockedBack = false;
        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = null;
        }

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
