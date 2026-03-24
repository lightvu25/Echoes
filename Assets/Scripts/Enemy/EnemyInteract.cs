using System;
using System.Collections;
using UnityEngine;

public class EnemyInteract : MonoBehaviour
{
    public static EnemyInteract Instance { get; private set; }

    public event EventHandler OnAttack;
    public event EventHandler OnNotice;
    public event EventHandler<OnStateArgs> OnStateChanged;

    public class OnStateArgs : EventArgs { public State state; }

    public enum State { Idle, Patrol, Notice, Chase, Attack }

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

    public bool isAttacking { get; private set; }

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
        if (targetPlayer != null)
        {
            var pHealth = targetPlayer.GetComponentInParent<HealthSystem>();
            if (pHealth == null) pHealth = targetPlayer.GetComponentInChildren<HealthSystem>();
            
            if (pHealth != null)
            {
                pHealth.OnDeath += HandlePlayerDeath;
            }
        }

        PickNewPatrolTarget();
        ChangeState(State.Patrol);
    }

    private void OnDestroy()
    {
        if (targetPlayer != null)
        {
            var pHealth = targetPlayer.GetComponentInParent<HealthSystem>();
            if (pHealth == null) pHealth = targetPlayer.GetComponentInChildren<HealthSystem>();

            if (pHealth != null)
            {
                pHealth.OnDeath -= HandlePlayerDeath;
            }
        }
    }

    private void HandlePlayerDeath(object sender, EventArgs e)
    {
        targetPlayer = null;
        isAttacking = false;
        
        StopAllCoroutines();
        if (enemyMovement != null) enemyMovement.Stop();
        
        GetComponent<EnemyAttack>()?.CancelHitbox();

        ChangeState(State.Idle);
    }

    private void OnEnable()
    {
        TickManager.onTick += OnTick;
    }

    private void OnDisable()
    {
        TickManager.onTick -= OnTick;
    }

    private void Update()
    {
        if (targetPlayer == null) return;

        // Lock all AI+movement during attack swing
        if (isAttacking)
        {
            enemyMovement.Stop();
            return;
        }

        // Accumulate timer every frame for precision (evaluated in OnTick)
        stateTimer += Time.deltaTime;

        // Drive movement/physics every frame based on current state
        switch (currentState)
        {
            case State.Patrol:  DrivePatrolMovement(); break;
            case State.Chase:   DriveChasement();      break;
            case State.Attack:  enemyMovement.Stop(); enemyMovement.CheckDirectionToFace(targetPlayer.position.x > transform.position.x); break;
            default:            enemyMovement.Stop(); break;
        }
    }

    // Called 5 times/sec — all expensive AI decisions live here
    private void OnTick()
    {
        if (targetPlayer == null || isAttacking) return;

        isPlayerVisible = CheckLineOfSight();

        switch (currentState)
        {
            case State.Idle:    TickIdle();    break;
            case State.Patrol:  TickPatrol();  break;
            case State.Notice:  TickNotice();  break;
            case State.Chase:   TickChase();   break;
            case State.Attack:  TickAttack();  break;
        }
    }

    private void ChangeState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;
        OnStateChanged?.Invoke(this, new OnStateArgs { state = newState });
    }

    // --- VISION (runs on tick) ---

    private bool CheckLineOfSight()
    {
        float distToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        if (distToPlayer <= data.closeDetectionRange) return true;

        if (distToPlayer <= data.visionRange)
        {
            Vector2 dirToPlayer = (targetPlayer.position - transform.position).normalized;
            Vector2 facingDir = enemyMovement.isFacingRight ? Vector2.right : Vector2.left;
            float angle = Vector2.Angle(facingDir, dirToPlayer);

            if (angle < data.fovAngle / 2f)
            {
                LayerMask allLayers = data.groundLayer | data.wallLayer | data.targetLayer;
                Vector2 origin = eyes != null ? (Vector2)eyes.position : (Vector2)transform.position;

                Collider2D playerCol = targetPlayer.GetComponent<Collider2D>();
                Vector2 targetPoint = playerCol != null ? (Vector2)playerCol.bounds.center : (Vector2)targetPlayer.position + Vector2.up * 0.5f;

                Vector2 dir = (targetPoint - origin).normalized;
                float dist = Vector2.Distance(origin, targetPoint);

                RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, allLayers);
                if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & data.targetLayer) != 0)
                    return true;
            }
        }

        return false;
    }

    // --- TICK STATE DECISIONS ---

    private void TickIdle()
    {
        if (isPlayerVisible) { ChangeState(State.Notice); return; }

        if (stateTimer >= UnityEngine.Random.Range(data.patrolWaitTimeMin, data.patrolWaitTimeMax))
        {
            if (!enemyMovement.isGroundedAhead || enemyMovement.isWallAhead)
                enemyMovement.CheckDirectionToFace(!enemyMovement.isFacingRight);

            PickNewPatrolTarget();
            ChangeState(State.Patrol);
        }
    }

    private void TickPatrol()
    {
        if (isPlayerVisible) { ChangeState(State.Notice); return; }

        if (!enemyMovement.isGroundedAhead || enemyMovement.isWallAhead)
        {
            ChangeState(State.Idle);
            return;
        }

        if (Mathf.Abs(transform.position.x - patrolTarget.x) < 0.5f)
            ChangeState(State.Idle);
    }

    private void TickNotice()
    {
        enemyMovement.CheckDirectionToFace(targetPlayer.position.x > transform.position.x);

        if (stateTimer == 0f) OnNotice?.Invoke(this, EventArgs.Empty);

        if (stateTimer >= data.noticeDuration)
        {
            float dist = Vector2.Distance(transform.position, targetPlayer.position);
            ChangeState(dist <= data.attackRange ? State.Attack : State.Chase);
        }
    }

    private void TickChase()
    {
        if (!isPlayerVisible) { ChangeState(State.Idle); return; }

        float dist = Vector2.Distance(transform.position, targetPlayer.position);
        if (dist <= data.attackRange) { ChangeState(State.Attack); return; }

        if (!enemyMovement.isGroundedAhead || enemyMovement.isWallAhead)
            ChangeState(State.Idle);
    }

    private void TickAttack()
    {
        float dist = Vector2.Distance(transform.position, targetPlayer.position);
        if (dist > data.attackRange + 0.5f) { ChangeState(State.Chase); return; }

        if (Time.time >= lastAttackTime + data.attackCooldown)
        {
            isAttacking = true;
            lastAttackTime = Time.time;
            OnAttack?.Invoke(this, EventArgs.Empty);
        }
    }

    // --- MOVEMENT DRIVERS (called from Update, frame-accurate) ---

    private void DrivePatrolMovement()
    {
        if (!enemyMovement.isGroundedAhead || enemyMovement.isWallAhead) return;

        float direction = Mathf.Sign(patrolTarget.x - transform.position.x);
        enemyMovement.Move(new Vector2(direction, 0), data.patrolMaxSpeed, data.patrolAccelAmount, data.patrolDeccelAmount);
    }

    private void DriveChasement()
    {
        if (!enemyMovement.isGroundedAhead || enemyMovement.isWallAhead) return;

        float direction = Mathf.Sign(targetPlayer.position.x - transform.position.x);
        enemyMovement.Move(new Vector2(direction, 0), data.chaseMaxSpeed, data.chaseAccelAmount, data.chaseDeccelAmount);
    }

    // --- ATTACK UNLOCK ---

    public void FinishAttack() => isAttacking = false;

    private IEnumerator AttackTimeoutFallback(float duration)
    {
        yield return new WaitForSeconds(duration);
        FinishAttack();
    }

    private void PickNewPatrolTarget()
    {
        float randomX = UnityEngine.Random.Range(-data.patrolRadius, data.patrolRadius);
        patrolTarget = new Vector2(startPos.x + randomX, startPos.y);
    }

    public bool IsPlayerOutsideVision()
    {
        if (targetPlayer == null) return true;
        return Vector2.Distance(transform.position, targetPlayer.position) > data.visionRange;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, data.closeDetectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)startPos : transform.position, data.patrolRadius);

        Vector3 eyePos = eyes != null ? eyes.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eyePos, eyePos + DirFromAngle(-data.fovAngle / 2, false) * data.visionRange);
        Gizmos.DrawLine(eyePos, eyePos + DirFromAngle(data.fovAngle / 2, false) * data.visionRange);
    }

    private Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            bool facing = enemyMovement != null ? enemyMovement.isFacingRight : transform.localScale.x > 0;
            angleInDegrees += facing ? 0f : 180f;
        }
        return new Vector3(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad));
    }
}
