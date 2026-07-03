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

    [SerializeField] private float backstepSpeed   = 6f;
    [SerializeField] private float backstepCooldown = 1.2f;
    private float _lastBackstepTime = -999f;
    private bool  _canBackstep;

    // Cached original base values from the cloned EnemyData (prevents multiplier compounding)
    private float _baseTelegraphDuration;
    private float _baseAttackCooldown;
    private float _baseVisionRange;

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
        // Clone the ScriptableObject so we can safely overwrite stats at runtime
        data = Instantiate(data);

        sensor = GetComponent<EnemySensor>();
        movement = GetComponent<IEnemyMovement>();
        attack = GetComponent<IEnemyAttack>();

        // Cache original base values before any evolution modifiers are applied
        _baseTelegraphDuration = data.telegraphDuration;
        _baseAttackCooldown    = data.attackCooldown;
        _baseVisionRange       = data.visionRange;
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

    private void OnEnable() { TickManager.onTick += OnTick; }
    private void OnDisable() { TickManager.onTick -= OnTick; }

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
            if (sensor.TargetPlayer == null || !IsTargetInHitboxZone())
                ChangeState(State.Chase);
            else
                ChangeState(State.Idle);
        }
    }

    private void Update()
    {
        // Guard against dead player
        if (PlayerStats.Instance != null)
        {
            var pHealth = PlayerStats.Instance.GetComponent<HealthSystem>();
            if (pHealth != null && pHealth.IsDead)
            {
                if (currentState != State.Idle)
                {
                    ChangeState(State.Idle);
                    if (attack != null) attack.CancelAttack();
                    if (movement != null) movement.Stop();
                    sensor.ClearTarget();
                }
                return;
            }
        }

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
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.Telegraph:
            case State.Attack:
                movement?.Stop();
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
        if (newState != State.Notice) hasNoticeFired = false;
        OnStateChanged?.Invoke(this, new OnStateArgs { state = newState });
    }

    public void ForceChase()
    {
        if (attack != null && attack.IsAttacking) return;
        ChangeState(State.Chase);
    }

    private void TickIdle()
    {
        if (sensor.IsPlayerVisible)
        {
            if (movement != null && (!movement.IsGroundedAhead || movement.IsWallAhead))
            {
                // Cannot proceed. Face player but stay in idle.
                if (sensor.TargetPlayer != null)
                {
                    bool faceRight = sensor.TargetPlayer.position.x > transform.position.x;
                    if (movement.IsFacingRight != faceRight)
                        movement.FaceDirection(faceRight);
                }
                return;
            }

            ChangeState(State.Chase);
            return;
        }

        if (Time.time >= lastAttackTime + Mathf.Max(0.1f, data.attackCooldown))
        {
            if (stateTimer >= UnityEngine.Random.Range(data.patrolWaitTimeMin, data.patrolWaitTimeMax))
            {
                if (movement != null && (!movement.IsGroundedAhead || movement.IsWallAhead))
                    movement.FaceDirection(!movement.IsFacingRight);

                PickNewPatrolTarget();

                if (movement != null)
                {
                    float dirToTarget = Mathf.Sign(patrolTarget.x - transform.position.x);
                    float facingDir = movement.IsFacingRight ? 1f : -1f;
                    if (dirToTarget != facingDir && dirToTarget != 0)
                        patrolTarget = new Vector2(transform.position.x + facingDir * UnityEngine.Random.Range(1f, data.patrolRadius), transform.position.y);
                }
                ChangeState(State.Patrol);
            }
        }
    }

    private void TickPatrol()
    {
        if (sensor.IsPlayerVisible) { ChangeState(State.Notice); return; }

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

            // Shared vision: notify all enemies that the player has been spotted
            EvolutionManager.Instance?.ReportPlayerSpotted(transform.position);
        }

        if (stateTimer >= data.noticeDuration)
        {
            ChangeState(IsTargetInHitboxZone() ? State.Telegraph : State.Chase);
        }
    }

    private bool IsTargetInHitboxZone()
    {
        if (sensor == null || sensor.TargetPlayer == null) return false;

        float dirX = sensor.TargetPlayer.position.x > transform.position.x ? 1f : -1f;
        Vector2 offset = new Vector2(data.attackHitboxOffset.x * dirX, data.attackHitboxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;

        Vector2 checkSize = data.attackHitboxSize;
        checkSize.x *= 0.66f; // Chỉ lấy 66% chiều dài => Quái phải tiến sâu vào 1/3 vùng hitbox mới đánh

        Collider2D hit = Physics2D.OverlapBox(center, checkSize, 0f, data.targetLayer);
        return hit != null;
    }

    private void TickChase()
    {
        if (!sensor.IsPlayerVisible)
        {
            ChangeState(State.Idle);
            return;
        }
    }

    private void TickTelegraph()
    {
        if (stateTimer >= data.telegraphDuration)
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
    }

    private void UpdateChase()
    {
        if (movement != null && (!movement.IsGroundedAhead || movement.IsWallAhead))
        {
            movement.Stop();
            ChangeState(State.Idle);
            return;
        }

        if (IsTargetInHitboxZone())
        {
            movement?.Stop();
            if (sensor.TargetPlayer != null)
            {
                movement?.FaceDirection(sensor.TargetPlayer.position.x > transform.position.x);
            }
            
            ChangeState(State.Telegraph);
        }
        else
        {
            DriveChase();
        }
    }

    private void UpdatePatrol()
    {
        if (movement != null && (!movement.IsGroundedAhead || movement.IsWallAhead))
        {
            movement.Stop();
            ChangeState(State.Idle);
        }
        else
        {
            DrivePatrol();
        }
    }

    private void DrivePatrol()
    {
        if (movement == null) return;

        float direction = Mathf.Sign(patrolTarget.x - transform.position.x);
        float mult = BurdenManager.Instance != null ? BurdenManager.Instance.CurrentSpeedMultiplier : 1f;
        movement.Move(new Vector2(direction, 0), data.patrolMaxSpeed * mult, data.patrolAccelAmount * mult, data.patrolDeccelAmount * mult);
    }

    private void DriveChase()
    {
        if (movement == null || sensor.TargetPlayer == null) return;

        float direction = Mathf.Sign(sensor.TargetPlayer.position.x - transform.position.x);
        float mult = BurdenManager.Instance != null ? BurdenManager.Instance.CurrentSpeedMultiplier : 1f;
        movement.Move(new Vector2(direction, 0), data.chaseMaxSpeed * mult, data.chaseAccelAmount * mult, data.chaseDeccelAmount * mult);
    }

    private void PickNewPatrolTarget()
    {
        float randomX = UnityEngine.Random.Range(-data.patrolRadius, data.patrolRadius);
        patrolTarget = new Vector2(startPos.x + randomX, startPos.y);
    }

    /// <summary>
    /// Applies evolution tier multipliers by overwriting the runtime-cloned EnemyData.
    /// Uses cached base values to prevent multiplier compounding on consecutive calls.
    /// </summary>
    public void ApplyEvolutionTier(EvolutionTierData tierData)
    {
        if (tierData == null) return;

        data.telegraphDuration = _baseTelegraphDuration * tierData.telegraphDurationMultiplier;
        data.attackCooldown    = _baseAttackCooldown * tierData.attackCooldownMultiplier;
        data.visionRange       = _baseVisionRange * tierData.visionRangeMultiplier;
        _canBackstep           = tierData.canBackstep;
    }
}