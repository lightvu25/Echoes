using System;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    public event EventHandler OnAttack;
    public event EventHandler OnNotice;
    public event EventHandler<OnStateArgs> OnStateChanged;

    public class OnStateArgs : EventArgs { public State state; }

    public enum State { Idle, Patrol, Notice, Chase, Telegraph, Attack }

    [SerializeField] private EnemyData data;

    [Header("Evolution")]
    public int levelIndex = 1;

    private float _telegraphDuration;
    private float _attackCooldown;
    private float _visionRange;
    private bool  _canBackstep;

    [SerializeField] private float backstepSpeed   = 6f;
    [SerializeField] private float backstepCooldown = 1.2f;
    private float _lastBackstepTime = -999f;
    private int   _evolutionTier;
    private int _chaseObstacleTicks = 0;
    private const int CHASE_OBSTACLE_THRESHOLD = 3;

    private EnemySensor sensor;
    private IEnemyMovement movement;
    private IEnemyAttack attack;

    private State currentState;
    private float stateTimer;
    private float lastAttackTime;
    private Vector2 startPos;
    private Vector2 patrolTarget;
    private bool hasNoticeFired;

    public State CurrentState => currentState;

    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();
        movement = GetComponent<IEnemyMovement>();
        attack = GetComponent<IEnemyAttack>();
    }

    private void Start()
    {
        startPos = transform.position;

        if (sensor.TargetPlayer != null)
        {
            var pHealth = sensor.TargetPlayer.GetComponentInParent<HealthSystem>();
            if (pHealth == null) pHealth = sensor.TargetPlayer.GetComponentInChildren<HealthSystem>();
            if (pHealth != null) pHealth.OnDeath += HandlePlayerDeath;
        }

        if (attack != null)
            attack.OnAttackFinished += HandleAttackFinished;

        PickNewPatrolTarget();
        ChangeState(State.Patrol);

        ApplyEvolutionModifiers();
    }

    private void OnDestroy()
    {
        if (sensor != null && sensor.TargetPlayer != null)
        {
            var pHealth = sensor.TargetPlayer.GetComponentInParent<HealthSystem>();
            if (pHealth == null) pHealth = sensor.TargetPlayer.GetComponentInChildren<HealthSystem>();
            if (pHealth != null) pHealth.OnDeath -= HandlePlayerDeath;
        }

        if (attack != null)
            attack.OnAttackFinished -= HandleAttackFinished;
    }

    private void OnEnable()
    {
        TickManager.onTick += OnTick;
    }

    private void OnDisable()
    {
        TickManager.onTick -= OnTick;
    }

    private void HandlePlayerDeath(object sender, EventArgs e)
    {
        sensor.ClearTarget();

        if (attack != null) attack.CancelAttack();
        if (movement != null) movement.Stop();

        ChangeState(State.Idle);
    }

    private void HandleAttackFinished(object sender, EventArgs e)
    {
        if (currentState == State.Attack || currentState == State.Telegraph)
        {
            float dist = sensor.DistanceToPlayer;
            if (sensor.TargetPlayer == null || dist > data.attackRange + 0.5f)
                ChangeState(State.Chase);
        }
    }

    private void Update()
    {
        if (sensor.TargetPlayer == null && currentState != State.Idle && currentState != State.Patrol)
        {
            movement?.Stop();
            return;
        }

        if (attack != null && attack.IsAttacking)
        {
            movement?.Stop();
            return;
        }

        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case State.Patrol:   DrivePatrol();    break;
            case State.Chase:    DriveChase();     break;
            case State.Telegraph:
            case State.Attack:
                movement?.Stop();
                if (sensor.TargetPlayer != null)
                    movement?.FaceDirection(sensor.TargetPlayer.position.x > transform.position.x);
                break;
            default:
                movement?.Stop();
                break;
        }

    }

    private void OnTick()
    {
        if (attack != null && attack.IsAttacking) return;

        switch (currentState)
        {
            case State.Idle:      TickIdle();      break;
            case State.Patrol:    TickPatrol();    break;
            case State.Notice:    TickNotice();    break;
            case State.Chase:     TickChase();     break;
            case State.Telegraph: TickTelegraph(); break;
            case State.Attack:    TickAttack();    break;
        }
    }

    private void ChangeState(State newState)
    {
        currentState = newState;
        stateTimer = 0f;
        _chaseObstacleTicks = 0;

        if (newState != State.Notice) hasNoticeFired = false;

        OnStateChanged?.Invoke(this, new OnStateArgs { state = newState });
    }

    public void ForceChase()
    {
        if (attack != null && attack.IsAttacking) return;
        ChangeState(State.Chase);
    }

    // --- Tick Decisions ---

    private void TickIdle()
    {
        if (sensor.IsPlayerVisible && !movement.IsWallAhead)
        {
            ChangeState(State.Chase);
            return;
        }

        if (sensor.IsPlayerVisible && movement.IsWallAhead)
        {
            return;
        }

        if (stateTimer >= UnityEngine.Random.Range(data.patrolWaitTimeMin, data.patrolWaitTimeMax))
        {
            if (movement != null && (!movement.IsGroundedAhead || movement.IsWallAhead))
                movement.FaceDirection(!movement.IsFacingRight);

            PickNewPatrolTarget();
            ChangeState(State.Patrol);
        }
    }

    private void TickPatrol()
    {
        if (sensor.IsPlayerVisible) { ChangeState(State.Notice); return; }

        if (movement != null && (!movement.IsGroundedAhead || movement.IsWallAhead))
        {
            movement.FaceDirection(!movement.IsFacingRight);
            float dir = movement.IsFacingRight ? 1f : -1f;
            patrolTarget = new Vector2(transform.position.x + dir * data.patrolRadius, transform.position.y);
            return;
        }

        if (Mathf.Abs(transform.position.x - patrolTarget.x) < 0.5f)
            ChangeState(State.Idle);
    }

    private void TickNotice()
    {
        if (sensor.TargetPlayer != null)
            movement?.FaceDirection(sensor.TargetPlayer.position.x > transform.position.x);

        if (!hasNoticeFired)
        {
            hasNoticeFired = true;
            OnNotice?.Invoke(this, EventArgs.Empty);
        }

        if (stateTimer >= data.noticeDuration)
        {
            ChangeState(sensor.DistanceToPlayer <= data.attackRange ? State.Telegraph : State.Chase);
        }
    }

    private void TickChase()
    {
        if (!sensor.IsPlayerVisible)
        {
            ChangeState(State.Idle);
            return;
        }

        if (movement != null && movement.IsWallAhead)
        {
            ChangeState(State.Idle);
            return;
        }

        if (sensor.DistanceToPlayer <= data.attackRange)
        {
            ChangeState(State.Telegraph);
            return;
        }
    }

    private void TickTelegraph()
    {
        if (sensor.TargetPlayer != null)
            movement?.FaceDirection(sensor.TargetPlayer.position.x > transform.position.x);

        if (stateTimer >= _telegraphDuration)
        {
            ChangeState(State.Attack);
            lastAttackTime = Time.time;
            OnAttack?.Invoke(this, EventArgs.Empty);
            attack?.ExecuteAttack();
        }
    }

    private void TickAttack()
    {
        if (_canBackstep && sensor.DistanceToPlayer < 1.5f
            && Time.time >= _lastBackstepTime + backstepCooldown
            && movement != null && movement.Rb != null)
        {
            float facingDir = movement.IsFacingRight ? 1f : -1f;
            movement.Rb.linearVelocity = new Vector2(-facingDir * backstepSpeed, movement.Rb.linearVelocity.y);
            _lastBackstepTime = Time.time;
        }

        if (Time.time >= lastAttackTime + Mathf.Max(0.1f, _attackCooldown))
            ChangeState(State.Telegraph);
    }

    // --- Movement Drivers ---

    private void DrivePatrol()
    {
        if (movement == null) return;

        if (!movement.IsGroundedAhead || movement.IsWallAhead) return;

        float direction = Mathf.Sign(patrolTarget.x - transform.position.x);
        movement.Move(new Vector2(direction, 0), data.patrolMaxSpeed, data.patrolAccelAmount, data.patrolDeccelAmount);
    }

    private void DriveChase()
    {
        if (movement == null || sensor.TargetPlayer == null) return;
        if (!movement.IsGroundedAhead || movement.IsWallAhead) return;

        float direction = Mathf.Sign(sensor.TargetPlayer.position.x - transform.position.x);
        movement.Move(new Vector2(direction, 0), data.chaseMaxSpeed, data.chaseAccelAmount, data.chaseDeccelAmount);
    }

    private void PickNewPatrolTarget()
    {
        float randomX = UnityEngine.Random.Range(-data.patrolRadius, data.patrolRadius);
        patrolTarget = new Vector2(startPos.x + randomX, startPos.y);
    }

    private void ApplyEvolutionModifiers()
    {
        // Seed defaults from the shared data asset.
        _telegraphDuration = data.telegraphDuration;
        _attackCooldown    = data.attackCooldown;
        _visionRange       = data.visionRange;
        _canBackstep       = false;

        if (GameDataManager.Instance != null)
        {
            int attempts = GameDataManager.Instance.GetLevelAttemptCount(levelIndex);
            _evolutionTier = Mathf.Clamp(attempts / 3, 0, 3);
        }

        // --- Forgotten_Hourglass Relic ---
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.TryGetComponent<InventoryManager>(out var inv) && inv.HasRelic("Forgotten_Hourglass"))
        {
            _evolutionTier = 0;
        }

        switch (_evolutionTier)
        {
            case 1:
                _telegraphDuration *= 0.6f;   // 40% faster windup
                break;
            case 2:
                _telegraphDuration *= 0.6f;
                _attackCooldown    *= 0.75f;  // 25% shorter cooldown
                _canBackstep        = true;
                break;
            case 3:
                _telegraphDuration *= 0.5f;   // 50% faster windup
                _attackCooldown    *= 0.75f;
                _canBackstep        = true;
                _visionRange       *= 1.3f;   // 30% larger aggro radius
                break;
        }

        // Push overrides to sibling components.
        var melee = GetComponent<MeleeAttack>();
        if (melee != null) melee.SetStartupDelay(_telegraphDuration * 0.5f);

        if (_evolutionTier >= 3)
        {
            var s = GetComponent<EnemySensor>();
            if (s != null) s.SetVisionRange(_visionRange);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)startPos : transform.position, data.patrolRadius);
        if (Application.isPlaying && _evolutionTier >= 3)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _visionRange);
        }
    }
}
