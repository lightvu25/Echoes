using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyRank
{
    Normal,
    Elite,
    Boss
}

[RequireComponent(typeof(HealthSystem))]
public class EnemyCombat : MonoBehaviour, IDamageable
{
    public static event Action<EnemyCombat, DamageInfo> OnAnyEnemyDied;
    public static event Action<EnemyCombat> OnEncounterActivated;
    public static event Action<EnemyCombat> OnEncounterDeactivated;

    private static readonly List<EnemyCombat> activeEncounters = new List<EnemyCombat>();

    public static IReadOnlyList<EnemyCombat> ActiveEncounters => activeEncounters;
    public event EventHandler<DamageReceivedArgs> OnDamageReceived;
    public event EventHandler OnEnemyDied;
    public class DamageReceivedArgs : EventArgs
    {
        public int damage;
        public Vector2 knockbackDir;
    }
    [Header("References")]
    [SerializeField] private EnemyData data;
    [SerializeField] private EnemyRank rank = EnemyRank.Normal;
    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;
    private HealthSystem healthSystem;
    private IEnemyMovement movement;
    private EnemyBrain brain;
    private Rigidbody2D rb;
    private bool isKnockedBack = false;
    private DamageInfo lastDamageInfo;
    private bool hasStarted;
    public bool IsDead => healthSystem != null && healthSystem.IsDead;
    public Transform Transform => transform;
    public float Defense => healthSystem != null ? healthSystem.Defense : 0f;
    public EnemyData Data => data;
    public EnemyRank Rank => rank;
    public bool IsEliteOrBoss => rank == EnemyRank.Elite || rank == EnemyRank.Boss;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        movement = GetComponent<IEnemyMovement>();
        brain = GetComponent<EnemyBrain>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (hasStarted)
            RegisterEncounter();
    }

    private void Start()
    {
        if (data != null && healthSystem != null)
        {
            float mult = BurdenManager.Instance != null ? BurdenManager.Instance.CurrentHealthMultiplier : 1f;
            healthSystem.SetMaxHP(Mathf.RoundToInt(data.maxHP * mult), true);
            healthSystem.SetDefense(data.defense);
        }
        if (healthSystem != null)
            healthSystem.OnDeath += HealthSystem_OnDeath;
            
        if (BurdenManager.Instance != null)
            BurdenManager.Instance.OnBurdenChanged += HandleBurdenChanged;

        hasStarted = true;
        RegisterEncounter();
    }

    private void OnDisable()
    {
        UnregisterEncounter();
    }

    private void HandleBurdenChanged(object sender, EventArgs e)
    {
        if (data != null && healthSystem != null)
        {
            float mult = BurdenManager.Instance != null ? BurdenManager.Instance.CurrentHealthMultiplier : 1f;
            healthSystem.SetMaxHP(Mathf.RoundToInt(data.maxHP * mult), false); 
        }
    }

    private void OnDestroy()
    {
        UnregisterEncounter();

        if (healthSystem != null)
            healthSystem.OnDeath -= HealthSystem_OnDeath;
            
        if (BurdenManager.Instance != null)
            BurdenManager.Instance.OnBurdenChanged -= HandleBurdenChanged;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (healthSystem == null || healthSystem.IsDead) return;
        if (healthSystem.IsInvincible && !damageInfo.BypassesInvincibilityFrames) return;

        lastDamageInfo = damageInfo;
        int appliedDamage = healthSystem.TakeDamage(damageInfo);
        if (appliedDamage <= 0) return;

        var attack = GetComponent<IEnemyAttack>();
        bool isCommittedToAttack = attack != null && attack.IsAttacking;

        if (brain != null && (brain.CurrentState == EnemyBrain.State.Telegraph || brain.CurrentState == EnemyBrain.State.Attack))
        {
            isCommittedToAttack = true;
        }

        if (damageInfo.isCritical) 
        {
            isCommittedToAttack = false;
        }
        
        if (!isCommittedToAttack)
        {
            if (attack != null) attack.CancelAttack();

            if (damageInfo.knockbackForce > 0f && rb != null)
            {
                ApplyKnockback(damageInfo.knockbackDirection, damageInfo.knockbackForce);
            }
            FaceAttacker(damageInfo.attacker);
        }

        OnDamageReceived?.Invoke(this, new DamageReceivedArgs
        {
            damage = appliedDamage,
            knockbackDir = damageInfo.knockbackDirection
        });
    }
    
    private void FaceAttacker(GameObject attacker)
    {
        if (attacker == null || movement == null) return;
        float directionX = attacker.transform.position.x - transform.position.x;
        bool attackerIsRight = directionX > 0f;
        if (attackerIsRight != movement.IsFacingRight)
            movement.FaceDirection(attackerIsRight);
        if (brain != null)
        {
            var state = brain.CurrentState;
            if (state == EnemyBrain.State.Idle || state == EnemyBrain.State.Patrol)
                brain.ForceChase();
        }
    }

    private void ApplyKnockback(Vector2 direction, float force)
    {
        if (isKnockedBack) return;
        StartCoroutine(KnockbackRoutine(direction, force));
    }

    public void ApplyExternalKnockback(Vector2 direction, float force)
    {
        ApplyKnockback(direction.normalized, force);
    }
    private System.Collections.IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        isKnockedBack = true;
        if (movement != null)
            movement.SetKnockedBack(true);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;
        if (movement != null)
            movement.SetKnockedBack(false);
    }
    private void HealthSystem_OnDeath(object sender, EventArgs e)
    {
        UnregisterEncounter();
        EvolutionManager.Instance?.RegisterKill();

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentLevelNoHitKills++;
        }
        OnEnemyDied?.Invoke(this, EventArgs.Empty);
        OnAnyEnemyDied?.Invoke(this, lastDamageInfo);
    }

    private void RegisterEncounter()
    {
        if (!IsEliteOrBoss || IsDead || activeEncounters.Contains(this)) return;

        activeEncounters.Add(this);
        OnEncounterActivated?.Invoke(this);
    }

    private void UnregisterEncounter()
    {
        if (!activeEncounters.Remove(this)) return;

        OnEncounterDeactivated?.Invoke(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEncounterRegistry()
    {
        activeEncounters.Clear();
        OnEncounterActivated = null;
        OnEncounterDeactivated = null;
    }

    public int CurrentHP => healthSystem != null ? healthSystem.CurrentHP : 0;
    public int MaxHP => healthSystem != null ? healthSystem.MaxHP : 0;
    public float HPPercent => healthSystem != null ? healthSystem.HPPercent : 0f;
    public bool IsKnockedBack => isKnockedBack;
}
