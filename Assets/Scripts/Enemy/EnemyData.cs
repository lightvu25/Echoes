using UnityEngine;

[System.Serializable]
public enum EnemyArchetype
{
    Melee,
    MidRange,
    Ranged,
    Flying,
    Bomber,
    Elite,
    Boss
}

[CreateAssetMenu(menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity & Balance Role")]
    [Tooltip("Design role used to keep this enemy's stats consistent with similar enemies.")]
    public EnemyArchetype archetype = EnemyArchetype.Melee;

    [Min(1)]
    [Tooltip("Relative balance tier inside the current biome. This is documentation only and does not scale stats at runtime.")]
    public int balanceTier = 1;

    [Header("Survivability")]
    [Min(1)]
    public int maxHP = 100;

    [Min(0f)]
    [Tooltip("Damage reduction uses Defense / (Defense + 100).")]
    public float defense = 0f;

    [Space(20)]

    [Header("Patrol & Navigation")]
    [Min(0.01f)]
    public float patrolMaxSpeed;

    [Min(0.01f)]
    public float patrolAcceleration;
    [HideInInspector] public float patrolAccelAmount;

    [InspectorName("Patrol Deceleration")]
    [Min(0.01f)]
    public float patrolDecceleration;
    [HideInInspector] public float patrolDeccelAmount;

    [Min(0f)]
    public float patrolRadius = 3f;

    [Min(0f)]
    public float patrolWaitTimeMin = 1f;

    [Min(0f)]
    public float patrolWaitTimeMax = 3f;

    [Min(0f)]
    public float groundCheckDistance = 1f;

    [Min(0f)]
    public float wallCheckDistance = 0.5f;

    [Space(20)]

    [Header("Chase Movement")]
    [Min(0.01f)]
    public float chaseMaxSpeed;

    [Min(0.01f)]
    public float chaseAcceleration;
    [HideInInspector] public float chaseAccelAmount;

    [InspectorName("Chase Deceleration")]
    [Min(0.01f)]
    public float chaseDecceleration;
    [HideInInspector] public float chaseDeccelAmount;

    [Tooltip("Preserves velocity above the target speed instead of immediately braking.")]
    public bool doConserveMomentum = true;

    [Space(20)]

    [Header("Attack Timing & Threat")]
    [Min(1)]
    [Tooltip("Base damage used by melee, dash, ranged projectile, and bomber attacks before difficulty scaling.")]
    public int attackBase;

    [Min(0.1f)]
    [Tooltip("Distance at which the EnemyBrain is allowed to begin its attack telegraph.")]
    public float attackRange;

    [Min(0.1f)]
    [Tooltip("Attack animation/action speed multiplier. 1 is normal speed.")]
    public float attackSpeed;

    [Min(0.1f)]
    public float attackCooldown;

    [Min(0f)]
    public float telegraphDuration = 0.5f;

    [Tooltip("Allows spacing-oriented enemies to retreat or kite at close range.")]
    public bool canBackstep;

    [Header("Contact Attack Hitbox")]
    [Tooltip("Used by contact/melee attacks. Projectile and explosion areas are configured by their attack components.")]
    public Vector2 attackHitboxSize = new Vector2(1.5f, 1.5f);
    public Vector2 attackHitboxOffset = new Vector2(0.75f, 0f);

    [Min(0f)]
    public float knockbackForce = 3f;
    public LayerMask attackTargetLayers;

    [Space(20)]

    [Header("Perception")]
    [Min(0.1f)]
    public float visionRange = 8f;

    [Range(0, 360)] public float fovAngle = 90f;

    [Min(0f)]
    public float closeDetectionRange = 1.5f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask targetLayer;

    [Min(0f)]
    public float noticeDuration = 0.5f;

    [Space(20)]

    [Header("Progression Rewards")]
    [Min(0)]
    public int expRewardMin = 10;

    [Min(0)]
    public int expRewardMax = 20;

    [Space(10)]
    [Range(0f, 100f)] public float goldDropChance = 50f;
    [Min(0)]
    public int goldAmountMin = 1;

    [Min(0)]
    public int goldAmountMax = 5;

    [Space(10)]
    [Range(0f, 100f)] public float astralShardDropChance = 10f;
    [Min(0)]
    public int astralShardAmountMin = 1;

    [Min(0)]
    public int astralShardAmountMax = 2;

    private void OnValidate()
    {
        maxHP = Mathf.Max(1, maxHP);
        defense = Mathf.Max(0f, defense);

        patrolMaxSpeed = Mathf.Max(0.01f, patrolMaxSpeed);
        patrolAcceleration = Mathf.Max(0.01f, patrolAcceleration);
        patrolDecceleration = Mathf.Max(0.01f, patrolDecceleration);
        patrolAccelAmount = 50f * patrolAcceleration / patrolMaxSpeed;
        patrolDeccelAmount = 50f * patrolDecceleration / patrolMaxSpeed;
        patrolRadius = Mathf.Max(0f, patrolRadius);
        patrolWaitTimeMin = Mathf.Max(0f, patrolWaitTimeMin);
        patrolWaitTimeMax = Mathf.Max(patrolWaitTimeMin, patrolWaitTimeMax);

        chaseMaxSpeed = Mathf.Max(0.01f, chaseMaxSpeed);
        chaseAcceleration = Mathf.Max(0.01f, chaseAcceleration);
        chaseDecceleration = Mathf.Max(0.01f, chaseDecceleration);
        chaseAccelAmount = 50f * chaseAcceleration / chaseMaxSpeed;
        chaseDeccelAmount = 50f * chaseDecceleration / chaseMaxSpeed;

        attackBase = Mathf.Max(1, attackBase);
        attackRange = Mathf.Max(0.1f, attackRange);
        attackSpeed = Mathf.Max(0.1f, attackSpeed);
        attackCooldown = Mathf.Max(0.1f, attackCooldown);
        telegraphDuration = Mathf.Max(0f, telegraphDuration);
        attackHitboxSize.x = Mathf.Max(0.05f, attackHitboxSize.x);
        attackHitboxSize.y = Mathf.Max(0.05f, attackHitboxSize.y);
        knockbackForce = Mathf.Max(0f, knockbackForce);

        visionRange = Mathf.Max(attackRange, visionRange);
        closeDetectionRange = Mathf.Clamp(closeDetectionRange, 0f, visionRange);
        noticeDuration = Mathf.Max(0f, noticeDuration);

        expRewardMin = Mathf.Max(0, expRewardMin);
        expRewardMax = Mathf.Max(expRewardMin, expRewardMax);
        goldAmountMin = Mathf.Max(0, goldAmountMin);
        goldAmountMax = Mathf.Max(goldAmountMin, goldAmountMax);
        astralShardAmountMin = Mathf.Max(0, astralShardAmountMin);
        astralShardAmountMax = Mathf.Max(astralShardAmountMin, astralShardAmountMax);

        targetLayer = LayerMask.GetMask("Player");
    }
}
