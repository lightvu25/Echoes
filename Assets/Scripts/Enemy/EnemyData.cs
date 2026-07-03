using UnityEngine;

[CreateAssetMenu(menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Health")]
    public int maxHP = 100;
    public float defense = 0f;

    [Space(20)]

    [Header("Patrol")]
    public float patrolMaxSpeed;
    public float patrolAcceleration;
    [HideInInspector] public float patrolAccelAmount;
    public float patrolDecceleration;
    [HideInInspector] public float patrolDeccelAmount;
    public float patrolRadius = 3f;
    public float patrolWaitTimeMin = 1f;
    public float patrolWaitTimeMax = 3f;

    // Check distance patrol
    public float groundCheckDistance = 1f;
    public float wallCheckDistance = 0.5f;

    [Space(20)]

    [Header("Chase")]
    public float chaseMaxSpeed;
    public float chaseAcceleration;
    [HideInInspector] public float chaseAccelAmount;
    public float chaseDecceleration;
    [HideInInspector] public float chaseDeccelAmount;

    public bool doConserveMomentum = true;

    [Space(20)]

    [Header("Attack")]
    public int attackBase;
    public float attackRange;
    public float attackSpeed;
    public float attackCooldown;
    public float telegraphDuration = 0.5f;

    [Header("Attack Hitbox")]
    public Vector2 attackHitboxSize = new Vector2(1.5f, 1.5f);
    public Vector2 attackHitboxOffset = new Vector2(0.75f, 0f);
    public float knockbackForce = 3f;
    public LayerMask attackTargetLayers;

    [Space(20)]

    [Header("Line of Sight")]
    public float visionRange = 8f;
    [Range(0, 360)] public float fovAngle = 90f;
    public float closeDetectionRange = 1.5f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask targetLayer;
    public float noticeDuration = 0.5f;

    [Space(20)]

    [Header("Loot Drops")]
    public int expRewardMin = 10;
    public int expRewardMax = 20;

    [Space(10)]
    [Range(0f, 100f)] public float goldDropChance = 50f;
    public int goldAmountMin = 1;
    public int goldAmountMax = 5;

    [Space(10)]
    [Range(0f, 100f)] public float astralShardDropChance = 10f;
    public int astralShardAmountMin = 1;
    public int astralShardAmountMax = 2;

    private void OnValidate()
    {
        patrolAccelAmount = (50 * patrolAcceleration) / patrolMaxSpeed;
        patrolDeccelAmount = (50 * patrolDecceleration) / patrolMaxSpeed;

        patrolAcceleration = Mathf.Clamp(patrolAcceleration, 0.01f, patrolMaxSpeed);
        patrolDecceleration = Mathf.Clamp(patrolDecceleration, 0.01f, patrolMaxSpeed);

        chaseAccelAmount = (50 * chaseAcceleration) / chaseMaxSpeed;
        chaseDeccelAmount = (50 * chaseDecceleration) / chaseMaxSpeed;

        chaseAcceleration = Mathf.Clamp(chaseAcceleration, 0.01f, chaseMaxSpeed);
        chaseDecceleration = Mathf.Clamp(chaseDecceleration, 0.01f, chaseMaxSpeed);
    }
}