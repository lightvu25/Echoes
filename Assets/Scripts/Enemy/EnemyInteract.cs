using System;
using UnityEngine;
using System.Collections;

public class EnemyInteract : MonoBehaviour
{
    public static EnemyInteract Instance { get; private set; }

    public event EventHandler OnAttack;
    public event EventHandler OnNotice;
    public event EventHandler<OnStateArgs> OnStateChanged;

    public class OnStateArgs : EventArgs { public State state; }

    public enum State
    {
        Idle,
        Patrol,
        Notice,
        Chase,
        Attack
    }

    private EnemyMovement enemyMovement;
    [SerializeField] private EnemyData data;
    [SerializeField] private Transform eyes;

    private State currentState;
    private Transform targetPlayer;

    private float stateTimer;
    private float lastAttackTime;
    private Vector2 startPos;
    private Vector2 patrolTarget;
    private bool isPlayerVisible;

    private void Awake()
    {
        Instance = this;

        enemyMovement = GetComponent<EnemyMovement>();
        startPos = transform.position;
        enemyMovement.Data = data;
    }

    private void Start()
    {
        targetPlayer = GameObject.FindGameObjectWithTag("Player")?.transform;
        PickNewPatrolTarget();
        ChangeState(State.Patrol);
    }

    private void Update()
    {
        if (targetPlayer == null) return;

        isPlayerVisible = CheckLineOfSight();

        switch (currentState)
        {
            case State.Idle:
                HandleIdle();
                break;
            case State.Patrol:
                HandlePatrol();
                break;
            case State.Notice:
                HandleNotice();
                break;
            case State.Chase:
                HandleChase();
                break;
            case State.Attack:
                HandleAttack();
                break;
        }

    }

    private void ChangeState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;
        OnStateChanged?.Invoke(this, new OnStateArgs { state = newState });
    }

    // --- HÀM XỬ LÝ VISION ---

    private bool CheckLineOfSight()
    {
        // Tính khoảng cách
        float distToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        // A. Cảm nhận gần (Nghe thấy tiếng bước chân/hơi thở)
        if (distToPlayer <= data.closeDetectionRange)
        {
            return true;
        }

        // B. Tầm nhìn xa (Mắt)
        if (distToPlayer <= data.visionRange)
        {
            Vector2 dirToPlayer = (targetPlayer.position - transform.position).normalized;
            Vector2 facingDir = enemyMovement.isFacingRight ? Vector2.right : Vector2.left;
            float angle = Vector2.Angle(facingDir, dirToPlayer);

            // 1. Kiểm tra xem Player có nằm trong góc nhìn không
            if (angle < data.fovAngle / 2f)
            {
                // 2. Tạo Mask: Bao gồm Đất, Tường VÀ CẢ PLAYER
                // (Để tia Raycast không xuyên qua Player)
                LayerMask allLayersToCheck = data.groundLayer | data.wallLayer | data.targetLayer;

                Vector2 origin = eyes != null ? (Vector2)eyes.position : (Vector2)transform.position;

                // MẸO: Bắn tia vào giữa người Player (Center) thay vì vào chân (Position)
                // để tránh tia bắn xuống đất bị vướng địa hình.
                // Nếu targetPlayer có Collider, hãy dùng bounds.center. Nếu không, cộng thêm vector Y.
                Collider2D playerCollider = targetPlayer.GetComponent<Collider2D>();
                Vector2 targetPoint = playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)targetPlayer.position + Vector2.up * 0.5f;

                Vector2 accurateDir = (targetPoint - origin).normalized;
                float accurateDist = Vector2.Distance(origin, targetPoint);

                // Bắn Raycast
                RaycastHit2D hit = Physics2D.Raycast(origin, accurateDir, accurateDist, allLayersToCheck);

                // 3. Xử lý kết quả Raycast
                if (hit.collider != null)
                {
                    // Kiểm tra xem vật bắn trúng có thuộc Layer của Player không?
                    // (Toán tử bitwise để check layer mask)
                    if (((1 << hit.collider.gameObject.layer) & data.targetLayer) != 0)
                    {
                        return true; // Bắn trúng Player -> Thấy!
                    }
                }
            }
        }

        return false;
    }

    // --- XỬ LÝ STATE ---

    private void HandleIdle()
    {
        enemyMovement.Stop();
        stateTimer += Time.deltaTime;

        if (isPlayerVisible)
        {
            ChangeState(State.Notice);
            return;
        }

        if (stateTimer >= UnityEngine.Random.Range(data.patrolWaitTimeMin, data.patrolWaitTimeMax))
        {
            if (!enemyMovement.isGroundedAhead || enemyMovement.isWallAhead)
            {
                enemyMovement.CheckDirectionToFace(!enemyMovement.isFacingRight);
            }

            PickNewPatrolTarget();
            ChangeState(State.Patrol);
        }
    }

    private void HandlePatrol()
    {
        if (isPlayerVisible)
        {
            ChangeState(State.Notice);
            return;
        }

        if (!enemyMovement.isGroundedAhead || enemyMovement.isWallAhead)
        {
            enemyMovement.Stop();
            ChangeState(State.Idle);
            return;
        }

        float distToTarget = Mathf.Abs(transform.position.x - patrolTarget.x);
        if (distToTarget < 0.5f)
        {
            ChangeState(State.Idle);
            return;
        }

        float direction = Mathf.Sign(patrolTarget.x - transform.position.x);
        Vector2 moveDir = new Vector2(direction, 0);

        enemyMovement.Move(moveDir, data.patrolMaxSpeed, data.patrolAccelAmount, data.patrolDeccelAmount);
    }

    private void HandleNotice()
    {
        enemyMovement.Stop();

        enemyMovement.CheckDirectionToFace(targetPlayer.position.x > transform.position.x);

        if (stateTimer == 0f)
        {
            OnNotice?.Invoke(this, EventArgs.Empty);
        }

        stateTimer += Time.deltaTime;

        if (stateTimer >= data.noticeDuration)
        {
            float dist = Vector2.Distance(transform.position, targetPlayer.position);
            if (dist <= data.attackRange)
            {
                ChangeState(State.Attack);
            }
            else
            {
                ChangeState(State.Chase);
            }
        }
    }

    private void HandleChase()
    {
        float dis = Vector2.Distance(transform.position, targetPlayer.position);

        if (!isPlayerVisible)
        {
            ChangeState(State.Idle);
            return;
        }

        if (dis <= data.attackRange)
        {
            ChangeState(State.Attack);
            return;
        }

        if (!enemyMovement.isGroundedAhead || enemyMovement.isWallAhead)
        {
            ChangeState(State.Idle);
            return;
        } 
        else
        {
            float direction = Mathf.Sign(targetPlayer.position.x - transform.position.x);
            float moveSpeed = data.chaseMaxSpeed;

            enemyMovement.Move(new Vector2(direction, 0), moveSpeed, data.chaseAccelAmount, data.chaseDeccelAmount);
        }
    }

    private void HandleAttack()
    {
        enemyMovement.Stop();
        enemyMovement.CheckDirectionToFace(targetPlayer.position.x > transform.position.x);

        float dist = Vector2.Distance(transform.position, targetPlayer.position);
        if (dist > data.attackRange + 0.5f) // Thêm chút buffer để đỡ bị flick
        {
            ChangeState(State.Chase);
            return;
        }

        if (Time.time >= lastAttackTime + data.attackCooldown)
        {
            OnAttack?.Invoke(this, EventArgs.Empty);
            lastAttackTime = Time.time;
        }
    }

    private void PickNewPatrolTarget()
    {
        float randomX = UnityEngine.Random.Range(-data.patrolRadius, data.patrolRadius);
        Vector2 potentialTarget = new Vector2(startPos.x + randomX, startPos.y);
        patrolTarget = potentialTarget;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Tầm nhìn xa
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);

        // Tầm nhìn gần
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, data.closeDetectionRange);

        // Tầm đánh
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);

        // Vùng đi tuần
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)startPos : transform.position, data.patrolRadius);
    
        // Hình nón FOV
        Vector3 eyePor = eyes != null ? eyes.position : transform.position;
        Vector3 viewAngleA = DirFromAngle(-data.fovAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(data.fovAngle / 2, false);

        Gizmos.color = Color.orange;
        Gizmos.DrawLine(eyePor, eyePor + viewAngleA * data.visionRange);
        Gizmos.DrawLine(eyePor, eyePor + viewAngleB * data.visionRange);
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += enemyMovement.isFacingRight ? 0f : 180f;
        }
        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad));
    }
}