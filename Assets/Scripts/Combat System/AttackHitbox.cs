using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public event EventHandler<HitEventArgs> OnHitTarget;

    public delegate void DamageModifier(IDamageable target, ref DamageInfo damageInfo);
    public event DamageModifier OnBeforeDamageApplied;

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
    public LayerMask TargetLayers => targetLayers;

    [Header("Timing")]
    [SerializeField] private float startupTime = 0.05f;
    [SerializeField] private float activeTime = 0.15f;
    [Tooltip("Interval between multi-hits (0 = single hit per target)")]
    [SerializeField] private float multiHitInterval = 0f;

    [Header("Damage")]
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float procCoefficient = 1f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float hitFreezeTime = 0.05f;

    [SerializeField] private CombatStats combatStats;

    private bool isActive = false;
    public bool isPlungeFalling = false;
    private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private Dictionary<IDamageable, float> multiHitTimers = new Dictionary<IDamageable, float>();
    private GameObject owner;
    private Collider2D[] hitResults = new Collider2D[20];

    private void Awake()
    {
        owner = transform.root.gameObject;
    }

    public void Activate()
    {
        if (isActive) Deactivate();
        StartCoroutine(HitboxRoutine());
    }

    public void Activate(int damage, float procCoef = 1f)
    {
        baseDamage = damage;
        procCoefficient = procCoef;
        Activate();
    }

    private int attackSequenceId;
    private bool hasPlayerAttackMetadata;
    private PlayerAttack.AttackType originatingAttackType;
    private PlaystyleType originatingPlaystyle;
    private int originatingComboStep;
    private bool originatedInAir;

    public void ConfigureAttackMetadata(int sequenceId, PlayerAttack.AttackType attackType, PlaystyleType style, int comboStep, bool inAir)
    {
        attackSequenceId = sequenceId;
        originatingAttackType = attackType;
        originatingPlaystyle = style;
        originatingComboStep = comboStep;
        originatedInAir = inAir;
        hasPlayerAttackMetadata = true;
    }

    public void Configure(Vector2 size, Vector2 offset)
    {
        hitboxSize = size;
        hitboxOffset = offset;
    }

    public void Deactivate()
    {
        isActive = false;
        StopAllCoroutines();
        hitTargets.Clear();
        multiHitTimers.Clear();
    }

    private void OnDisable()
    {
        isActive = false;
        hitTargets.Clear();
        multiHitTimers.Clear();
    }

    private IEnumerator HitboxRoutine()
    {
        if (startupTime > 0f) yield return new WaitForSeconds(startupTime);

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

        isActive = false;
        hitTargets.Clear();
        multiHitTimers.Clear();
    }

    private void CheckHits()
    {
        if (!isActive) return;

        Vector2 direction = owner.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * direction.x, hitboxOffset.y);

        int hitCount = Physics2D.OverlapBoxNonAlloc(center, hitboxSize, 0f, hitResults, targetLayers);

        for (int i = 0; i < hitCount; i++)
        {
            var hit = hitResults[i];
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDead) continue;

            if (multiHitInterval > 0f)
            {
                if (multiHitTimers.TryGetValue(target, out float lastHit) && Time.time - lastHit < multiHitInterval) continue;
                multiHitTimers[target] = Time.time;
            }
            else
            {
                if (hitTargets.Contains(target)) continue;
                hitTargets.Add(target);
            }

            ApplyDamage(target);
        }
    }

    private void ApplyDamage(IDamageable target)
    {
        Vector2 knockbackDir = (target.Transform.position - transform.position).normalized;
        if (knockbackDir == Vector2.zero)
            knockbackDir = owner.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;

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
            damageSource = DamageSourceType.Attack,
            isCritical = false,
            attackSequenceId = this.attackSequenceId,
            hasPlayerAttackMetadata = this.hasPlayerAttackMetadata,
            originatingAttackType = this.originatingAttackType,
            originatingPlaystyle = this.originatingPlaystyle,
            originatingComboStep = this.originatingComboStep,
            originatedInAir = this.originatedInAir
        };

        // Populate echo and level data if the attacker is the player
        if (owner.CompareTag("Player"))
        {
            if (PlayerInventoryCore.Instance != null)
                damageInfo.activeEcho = PlayerInventoryCore.Instance.GetActiveEcho();
            
            if (PlayerStats.Instance != null)
                damageInfo.playerLevel = PlayerStats.Instance.CurrentLevel;
        }

        if (combatStats != null && UnityEngine.Random.value < combatStats.critChance)
        {
            damageInfo.isCritical = true;
            damageInfo.multiplicativeStack *= combatStats.critMultiplier;
        }

        OnBeforeDamageApplied?.Invoke(target, ref damageInfo);

        int finalDamage = DamageCalculator.CalculateFinalDamage(damageInfo, target.Defense);
        
        // Debug Log for Testing
        if (damageInfo.activeEcho != null)
        {
            Debug.Log($"[Damage Test] Hit {target.Transform.name} | " +
                      $"Base: {baseDamage} | " +
                      $"Echo: {damageInfo.activeEcho.itemName} (Lvl {damageInfo.playerLevel}) | " +
                      $"Final: {finalDamage}");
        }

        HealthSystem targetHealth = target.Transform != null
            ? target.Transform.GetComponentInParent<HealthSystem>()
            : null;
        int hpBeforeHit = targetHealth != null ? targetHealth.CurrentHP : -1;

        target.TakeDamage(damageInfo);

        // Only play impact feedback for an accepted hit.  This prevents attacks
        // rejected by i-frames from shaking the camera as though they connected.
        int appliedDamage = targetHealth != null
            ? Mathf.Max(0, hpBeforeHit - targetHealth.CurrentHP)
            : finalDamage;
        GameFeelManager.Instance?.ProcessHit(
            owner,
            target.Transform != null ? target.Transform.gameObject : null,
            damageInfo,
            appliedDamage);

        InvokeOnHitTarget(target, damageInfo, finalDamage);
    }

    public void InvokeBeforeDamageApplied(IDamageable target, ref DamageInfo info)
    {
        OnBeforeDamageApplied?.Invoke(target, ref info);
    }

    public void InvokeOnHitTarget(IDamageable target, DamageInfo info, int finalDamage)
    {
        if (info.activeEcho != null && info.activeEcho.hitImpactPrefab != null && target != null)
        {
            ObjectPoolManager.SpawnObject(info.activeEcho.hitImpactPrefab, target.Transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
        }

        OnHitTarget?.Invoke(this, new HitEventArgs { target = target, damageInfo = info, finalDamage = finalDamage });
    }

    private void OnDrawGizmos()
    {
        GameObject root = transform.root.gameObject;
        Vector2 direction = root.transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset.x * direction.x, hitboxOffset.y);

        Gizmos.color = (isActive || isPlungeFalling) ? new Color(1f, 0f, 0f, 0.4f) : new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(center, hitboxSize);

        Gizmos.color = (isActive || isPlungeFalling) ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(center, hitboxSize);
    }
}
