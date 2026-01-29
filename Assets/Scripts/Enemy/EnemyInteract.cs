using System;
using UnityEngine;

public class EnemyInteract : MonoBehaviour
{
    public static EnemyInteract Instance { get; private set; }

    public event EventHandler OnAttack;

    [SerializeField] private EnemyData data;
    
    private EnemyMovement enemyMovement;
    private Transform player;
    private float lastAttackTime;

    private Vector2 wanderTarget;
    private float wanderWaitTimer;
    private bool isWaiting;
    private Vector2 startPosition;

    // Gizmos
    private Vector2 debugSteeringDirection;
    private bool debugCanSeePlayer;

    private void Awake()
    {
        Instance = this;
        enemyMovement = GetComponent<EnemyMovement>();
        enemyMovement.Data = data; 
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        startPosition = transform.position;
        PickNewWanderTarget();
    }

    private void FixedUpdate()
    {
        if (player == null || enemyMovement.isKnockedBack) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        debugCanSeePlayer = CheckLineOfSight(distanceToPlayer);

        if (debugCanSeePlayer)
        {
            isWaiting = false;
            if (distanceToPlayer <= data.attackRange)
            {
                PerformAttack();
            }
            else
            {
                PerformChase();
            }
        }
        else
        {
            PerformWander();
        }
    }

    private void PerformChase()
    {
        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        
        Vector2 steering = GetObstacleAvoidance(directionToPlayer);
        debugSteeringDirection = steering;

        enemyMovement.Move(steering, data.runMaxSpeed, data.runAccelAmount, data.runDeccelAmount);
    }

    private void PerformWander()
    {
        if (isWaiting)
        {
            enemyMovement.Move(Vector2.zero, 0, 0, 0);
            wanderWaitTimer -= Time.fixedDeltaTime;

            if (wanderWaitTimer == 0)
            {
                isWaiting = false;
                PickNewWanderTarget();
            }
        }
        else
        {
            // Tính hướng đi tới điểm đích
            Vector2 directionToTarget = (wanderTarget - (Vector2)transform.position).normalized;
            Vector2 moveDir = new Vector2(Mathf.Sign(wanderTarget.x - transform.position.x), 0);

            // Kiểm tra xem đã đến nơi chưa (Sai số 0.5f)
            if (Mathf.Abs(transform.position.x - wanderTarget.x) < 0.5f)
            {
                isWaiting = true;
                wanderWaitTimer = UnityEngine.Random.Range(data.wanderWaitTimeMin, data.wanderWaitTimeMax);
            }
            else
            {
                // Dùng ObstacleAvoidance để tránh đâm đầu vào tường khi đi tuần
                Vector2 steering = GetObstacleAvoidance(moveDir);
                enemyMovement.Move(steering, data.wanderMaxSpeed, data.wanderAccelAmount, data.wanderDeccelAmount);
            }
        }
    }

    private void PickNewWanderTarget()
    {
        float randomX = startPosition.x + UnityEngine.Random.Range(-data.wanderRadius, data.wanderRadius);
        wanderTarget = new Vector2(randomX, transform.position.y);
    }

    private void PerformAttack()
    {
        enemyMovement.Move(Vector2.zero, 0, 0, 0);

        if (Time.time > lastAttackTime + data.attackCooldown)
        {
            OnAttack?.Invoke(this, EventArgs.Empty);
            lastAttackTime = Time.time;
        }
    }

    private Vector2 GetObstacleAvoidance(Vector2 desiredDirection)
    {
        Vector2[] rayDirections = { 
            desiredDirection, 
            Quaternion.Euler(0, 0, 45) * desiredDirection, 
            Quaternion.Euler(0, 0, -45) * desiredDirection 
        };

        Vector2 finalDirection = desiredDirection;

        foreach (var dir in rayDirections)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, 1.5f, data.obstacleLayer);
            if (hit.collider != null)
            {
                Vector2 avoidForce = (Vector2)transform.position - hit.point;
                finalDirection += avoidForce.normalized * 2f;
            }
        }
        return finalDirection.normalized;
    }

    private bool CheckLineOfSight(float dist)
    {
        if (dist <= data.perceptionRange)
             return !Physics2D.Raycast(transform.position, (player.position - transform.position).normalized, dist, data.obstacleLayer);

        if (dist <= data.visionRange)
        {
            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            Vector2 facingDir = enemyMovement.isFacingRight ? Vector2.right : Vector2.left;

            float angle = Vector2.Angle(facingDir, dirToPlayer);
            if (angle < data.fovAngle / 2f)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, dist, data.obstacleLayer);
                return hit.collider == null;
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (data == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.visionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, debugSteeringDirection * 2);
    }
}