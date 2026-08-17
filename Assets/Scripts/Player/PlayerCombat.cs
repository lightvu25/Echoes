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


    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;

    private HealthSystem healthSystem;
    private PlayerAttack playerAttack;
    private PlayerMovement playerMovement;
    private Rigidbody2D rb;
    private Coroutine _knockbackCoroutine;
    private bool isKnockedBack = false;

    public bool IsDead => healthSystem != null && healthSystem.IsDead;
    public Transform Transform => transform;
    public float Defense => healthSystem != null ? healthSystem.Defense : 0f;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        playerAttack = GetComponent<PlayerAttack>();
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
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

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (healthSystem == null || healthSystem.IsDead) return;

        int finalDamage = healthSystem.TakeDamage(damageInfo);

        // Invulnerability and fully-negated hits must not stagger, knock back,
        // cancel attacks, or produce player-hit feedback.
        if (finalDamage <= 0) return;

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentLevelNoHitKills = 0;
        }

        if (damageInfo.suppressHitReaction || healthSystem.LastHitReactionSuppressed)
        {
            GameFeelManager.Instance?.ProcessPlayerHit(transform.position, damageInfo.knockbackDirection);
            OnDamageReceived?.Invoke(this, new DamageReceivedArgs
            {
                damage = finalDamage,
                knockbackDir = Vector2.zero
            });
            return;
        }

        // Always apply knockback/stagger to cancel attacks and briefly stop movement, 
        // even if the incoming knockback force is 0.
        if (rb != null)
        {
            ApplyKnockback(damageInfo.knockbackDirection, damageInfo.knockbackForce);
        }

        if (playerMovement != null)
        {
            playerMovement.ApplyStun(0.3f);
        }

        GameFeelManager.Instance?.ProcessPlayerHit(transform.position, damageInfo.knockbackDirection);

        OnDamageReceived?.Invoke(this, new DamageReceivedArgs
        {
            damage = finalDamage,
            knockbackDir = damageInfo.knockbackDirection
        });
    }

    private void ApplyKnockback(Vector2 direction, float force)
    {
        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            isKnockedBack = false;
        }

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

        if (GameSession.Instance != null)
        {
            GameSession.Instance.HandlePlayerDeath();
        }

        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.Dead();
        }
    }

    public int CurrentHP => healthSystem != null ? healthSystem.CurrentHP : 0;
    public int MaxHP => healthSystem != null ? healthSystem.MaxHP : 0;

    public float HPPercent => healthSystem != null ? healthSystem.HPPercent : 0f;
    public bool IsKnockedBack => isKnockedBack;
}
