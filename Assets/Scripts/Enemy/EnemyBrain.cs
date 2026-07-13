using System;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    public event EventHandler OnAttack;
    public event EventHandler OnNotice;
    public event EventHandler<OnStateArgs> OnStateChanged;

    public class OnStateArgs : EventArgs { public State state; }

    public enum State { Idle, Patrol, Notice, Chase, Telegraph, Attack, Backstep }

    [SerializeField] private EnemyData data;
    public EnemyData Data => data;

    [SerializeField] private float backstepSpeed   = 6f;
    [SerializeField] private float backstepCooldown = 1.2f;
    private float _lastBackstepTime = -999f;
    private bool  _canBackstep;
    public bool CanBackstep { get => _canBackstep; set => _canBackstep = value; }

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
        
        _canBackstep = data.canBackstep;
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
        lastAttackTime = Time.time; // Reset cooldown from the END of the attack

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
            case State.Backstep:
                if (currentState != State.Backstep) movement?.Stop();
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
            case State.Backstep:  TickBackstep();  break;
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

    public void ForceNotice()
    {
        if (attack != null && attack.IsAttacking) return;
        // Don't restart notice if already in it
        if (currentState == State.Notice) return;
        ChangeState(State.Notice);
    }

    private void TickIdle()
    {
        if (sensor.IsPlayerVisible)
        {
            if (IsTargetInHitboxZone())
            {
                // If target is already in attack range, we don't care if there's a wall/ledge in front of us.
                // Go to Chase, which will instantly trigger Telegraph.
                ChangeState(State.Chase);
                return;
            }

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
            float waitTime = Mathf.Max(0.2f, UnityEngine.Random.Range(data.patrolWaitTimeMin, data.patrolWaitTimeMax));
            if (stateTimer >= waitTime)
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
        checkSize.x *= 0.66f; // Take 66% to require enemy to get deep into hitbox

        Collider2D hit = Physics2D.OverlapBox(center, checkSize, 0f, data.targetLayer);

        // If the player is inside the "minimum range deadzone" (closer than the hitbox), 
        // we should still allow ranged enemies to attack instead of just standing still!
        if (hit == null && data.attackHitboxOffset.x > 1f)
        {
            float farEdge = data.attackHitboxOffset.x + (data.attackHitboxSize.x / 2f);
            if (sensor.DistanceToPlayer <= farEdge)
            {
                return true;
            }
        }

        return hit != null;
    }

    private void TickChase()
    {
        if (!sensor.IsPlayerVisible || sensor.TargetPlayer == null)
        {
            ChangeState(State.Idle);
            return;
        }
    }

    private void TickTelegraph()
    {
        if (_canBackstep && sensor.DistanceToPlayer <= 3f && Time.time >= _lastBackstepTime + backstepCooldown)
        {
            _lastBackstepTime = Time.time;
            ChangeState(State.Backstep);
            return;
        }

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
        // Attack logic handled by IEnemyAttack components
    }

    private void TickBackstep()
    {
        if (stateTimer >= 0.3f) // Fixed 0.3s backstep duration
        {
            movement?.Stop();
            ChangeState(State.Telegraph);
        }
        else
        {
            if (movement != null && movement.Rb != null)
            {
                if (sensor.TargetPlayer != null)
                {
                    bool faceRight = sensor.TargetPlayer.position.x > transform.position.x;
                    if (movement.IsFacingRight != faceRight) movement.FaceDirection(faceRight);
                }

                float facingDir = movement.IsFacingRight ? 1f : -1f;
                movement.Rb.linearVelocity = new Vector2(-facingDir * backstepSpeed, movement.Rb.linearVelocity.y);
            }
        }
    }

    private float _kiteStuckCooldown = 0f;

    private void UpdateChase()
    {
        if (IsTargetInHitboxZone())
        {
            movement?.Stop();
            if (sensor.TargetPlayer != null)
            {
                movement?.FaceDirection(sensor.TargetPlayer.position.x > transform.position.x);
            }
            
            if (Time.time >= lastAttackTime + Mathf.Max(0.1f, data.attackCooldown))
            {
                ChangeState(State.Telegraph);
            }
            return;
        }

        if (_canBackstep && sensor.DistanceToPlayer <= 3f && Time.time >= _lastBackstepTime + backstepCooldown)
        {
            _lastBackstepTime = Time.time;
            ChangeState(State.Backstep);
            return;
        }

        if (movement != null && (!movement.IsGroundedAhead || movement.IsWallAhead))
        {
            movement.Stop();
            if (sensor.TargetPlayer != null)
            {
                bool faceRight = sensor.TargetPlayer.position.x > transform.position.x;
                if (movement.IsFacingRight != faceRight)
                {
                    // We hit a wall/ledge while facing AWAY from the player (kiting).
                    // Stop kiting for a bit to prevent rapid flipping.
                    _kiteStuckCooldown = Time.time + 0.5f;
                    movement.FaceDirection(faceRight);
                    return;
                }
            }
            
            ChangeState(State.Idle);
            return;
        }

        DriveChase();
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

        Vector2 direction;
        if (movement is FlyingMovement)
        {
            direction = (patrolTarget - (Vector2)transform.position).normalized;
        }
        else
        {
            direction = new Vector2(Mathf.Sign(patrolTarget.x - transform.position.x), 0);
        }
        
        float mult = BurdenManager.Instance != null ? BurdenManager.Instance.CurrentSpeedMultiplier : 1f;
        movement.Move(direction, data.patrolMaxSpeed * mult, data.patrolAccelAmount * mult, data.patrolDeccelAmount * mult);
    }

    private void DriveChase()
    {
        Vector2 direction;
        float optimalDistance = Mathf.Max(3f, data.attackHitboxOffset.x);
        bool shouldKite = _canBackstep && sensor.DistanceToPlayer <= optimalDistance && !IsTargetInHitboxZone() && Time.time >= _kiteStuckCooldown;
        
        if (movement is FlyingMovement)
        {
            direction = (sensor.TargetPlayer.position - transform.position).normalized;
            if (shouldKite)
            {
                direction = -direction; // Kite away
            }
        }
        else
        {
            float dirX = Mathf.Sign(sensor.TargetPlayer.position.x - transform.position.x);
            if (shouldKite)
            {
                dirX = -dirX; // Kite away
            }
            direction = new Vector2(dirX, 0);
        }

        float mult = BurdenManager.Instance != null ? BurdenManager.Instance.CurrentSpeedMultiplier : 1f;
        movement.Move(direction, data.chaseMaxSpeed * mult, data.chaseAccelAmount * mult, data.chaseDeccelAmount * mult);
    }

    private void PickNewPatrolTarget()
    {
        float randomX = UnityEngine.Random.Range(-data.patrolRadius, data.patrolRadius);
        patrolTarget = new Vector2(startPos.x + randomX, startPos.y);
    }

    public void ApplyEvolutionTier(EvolutionTierData tierData)
    {
        if (tierData == null) return;

        data.telegraphDuration = _baseTelegraphDuration * tierData.telegraphDurationMultiplier;
        data.attackCooldown    = _baseAttackCooldown * tierData.attackCooldownMultiplier;
        data.visionRange       = _baseVisionRange * tierData.visionRangeMultiplier;
        _canBackstep           = data.canBackstep || tierData.canBackstep;
    }
}