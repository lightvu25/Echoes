using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] private Transform eyes;

    private IEnemyMovement movement;
    private Transform targetPlayer;

    public bool IsPlayerVisible { get; private set; }
    public float DistanceToPlayer { get; private set; }
    public Transform TargetPlayer => targetPlayer;

    private bool isGlobalAggroActive;

    /// <summary>
    /// Activates global aggro mode — bypasses all raycast, distance, and FOV checks,
    /// immediately reporting the player as visible.
    /// </summary>
    public void TriggerGlobalAggro() => isGlobalAggroActive = true;

    private void Awake()
    {
        movement = GetComponent<IEnemyMovement>();
    }

    private void Start()
    {
        targetPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void OnEnable()
    {
        TickManager.onTick += OnTick;
    }

    private void OnDisable()
    {
        TickManager.onTick -= OnTick;
    }

    private void OnTick()
    {
        if (targetPlayer == null)
        {
            IsPlayerVisible = false;
            DistanceToPlayer = float.MaxValue;
            return;
        }

        IDamageable playerDamageable = targetPlayer.GetComponent<IDamageable>();
        if (playerDamageable != null && playerDamageable.IsDead)
        {
            IsPlayerVisible = false;
            DistanceToPlayer = float.MaxValue;
            return;
        }

        // Global aggro: skip all checks, player is always "visible"
        if (isGlobalAggroActive)
        {
            DistanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
            IsPlayerVisible = true;
            return;
        }

        DistanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
        IsPlayerVisible = CheckLineOfSight();
    }

    private bool CheckLineOfSight()
    {
        if (DistanceToPlayer <= data.closeDetectionRange) return true;

        if (DistanceToPlayer <= data.visionRange)
        {
            Vector2 dirToPlayer = (targetPlayer.position - transform.position).normalized;
            bool facingRight = movement != null ? movement.IsFacingRight : transform.localScale.x > 0;
            Vector2 facingDir = facingRight ? Vector2.right : Vector2.left;
            float angle = Vector2.Angle(facingDir, dirToPlayer);

            if (angle < data.fovAngle / 2f)
            {
                LayerMask mask = data.groundLayer | data.wallLayer | data.targetLayer;
                Vector2 origin = eyes != null ? (Vector2)eyes.position : (Vector2)transform.position;

                Collider2D playerCol = targetPlayer.GetComponent<Collider2D>();
                Vector2 targetPoint = playerCol != null
                    ? (Vector2)playerCol.bounds.center
                    : (Vector2)targetPlayer.position + Vector2.up * 0.5f;

                Vector2 dir = (targetPoint - origin).normalized;
                float dist = Vector2.Distance(origin, targetPoint);

                RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, mask);
                if (hit.collider != null)
                {
                    if (hit.collider.transform.root == transform.root) return false;
                    return ((1 << hit.collider.gameObject.layer) & data.targetLayer) != 0;
                }
            }
        }

        return false;
    }

    public bool IsPlayerOutsideVision()
    {
        if (targetPlayer == null) return true;
        return DistanceToPlayer > data.visionRange;
    }

    public void ClearTarget()
    {
        targetPlayer = null;
        IsPlayerVisible = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, data.closeDetectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);

        Vector3 eyePos = eyes != null ? eyes.position : transform.position;
        bool facingRight = movement != null ? movement.IsFacingRight : transform.localScale.x > 0;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eyePos, eyePos + DirFromAngle(-data.fovAngle / 2, facingRight) * data.visionRange);
        Gizmos.DrawLine(eyePos, eyePos + DirFromAngle(data.fovAngle / 2, facingRight) * data.visionRange);
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool facingRight)
    {
        angleInDegrees += facingRight ? 0f : 180f;
        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad));
    }
}
