using System;
using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public event EventHandler OnIdle;
    public event EventHandler OnJump;
    public event EventHandler OnLand;
    public event EventHandler OnRun;
    public event EventHandler OnDash;
    public event EventHandler OnStopRun;
    public event EventHandler OnCling;
    public event EventHandler OnGrab;
    public event EventHandler OnGetup;
    public event EventHandler OnFall;

    public PlayerData Data;
    private PlayerCombat playerCombat;
    private PlayerAttack playerAttack;
    private HealthSystem healthSystem;
    private InventoryManager inventoryManager;
    private FallDamageHandler _fallDamageHandler;

    [Header("Phantom Shadow")]
    [SerializeField] private GameObject phantomShadowPrefab;
    private GameObject _phantomShadowAnchor;
    
    [Header("Space Claw")]
    [SerializeField] private float spaceClawKnockupForce = 15f;
    [SerializeField] private float spaceClawHitRadius = 2f;

    [Header("Input")]
    [SerializeField] private InputConfig inputConfig;

    [Header("One Way Platform")]
    [SerializeField] private Collider2D playerCollider;

    public Rigidbody2D rb { get; private set; }

    public bool isFacingRight    { get; private set; }
    public bool isJumping        { get; private set; }
    public bool isWallJumping    { get; private set; }
    public bool isDashing        { get; private set; }
    public bool isSliding        { get; private set; }
    public bool isLedgeGrabbing  { get; private set; }
    public bool isClimbing       { get; private set; }
    public bool isPlunging       { get; private set; }

    public float LastOnGroundTime     { get; private set; }
    public float LastOnWallTime       { get; private set; }
    public float LastOnWallRightTime  { get; private set; }
    public float LastOnWallLeftTime   { get; private set; }
    public float LastDashTime         { get; private set; }

    private int   _jumpsLeft;
    private bool  _isJumpCut;
    private bool _isJumpFalling;

    private float _wallJumpStartTime;
    private int   _lastWallJumpDir;
    
    private float _lastJumpExecTime;

    private int    _dashesLeft;
    private bool   _dashRefilling;
    private Vector2 _lastDashDir;
    private bool   _isDashAttacking;

    private Vector2 _moveInput;
    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }

    private bool _wasGrounded;
    private bool _wasMoving;
    private bool isDead = false;

    private float _waterSpeedMultiplier = 1f;
    private float _defaultLinearDrag;

    private bool _isTouchingClimbable;

    private bool _canLedgeGrab = true;
    private bool _ledgeCoroutineRunning = false;

    public bool isGrounded => LastOnGroundTime > 0;
    public bool isRunning => Mathf.Abs(rb.linearVelocity.x) > 0.1f && isGrounded;

    [Header("Checks")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private Vector2   _groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)]
    [SerializeField] private Transform _fromWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2   _wallCheckSize = new Vector2(0.5f, 1f);

    [Header("Ledge Climb")]
    [SerializeField] private Transform _ledgeCheck;
    [SerializeField] private Vector2   _ledgeCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private float     _ledgeClimbXOffset = 0.5f;
    [SerializeField] private float     _ledgeClimbYOffset = 1f;
    [SerializeField] private float     ledgeGrabDuration = 0.5f;

    [Header("Climbing")]
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private float wallSlideSpeed = 2f;

    [Header("Plunge Attack")]
    [SerializeField] private float plungeSpeed = 30f;

    [Header("Drop Through Platform")]
    [SerializeField] private float dropTime = 0.2f;

    [Header("Layers & Tag")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _wallLayer;

    // ------------------------------------------------------------------ //
    //  Unity Lifecycle                                                     //
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        rb            = GetComponent<Rigidbody2D>();
        playerCombat  = GetComponent<PlayerCombat>();
        playerAttack  = GetComponent<PlayerAttack>();
        healthSystem  = GetComponent<HealthSystem>();
        inventoryManager = GetComponent<InventoryManager>();
        _fallDamageHandler = GetComponent<FallDamageHandler>();
        _defaultLinearDrag = rb.linearDamping;
    }

    private void Start()
    {
        SetGravityScale(Data.gravityScale);
        isFacingRight = transform.localScale.x > 0;
        PlayerInteract.Instance.OnDead += PlayerInteract_OnDead;
    }

    private void Update()
    {
        LastOnGroundTime     -= Time.deltaTime;
        LastOnWallTime       -= Time.deltaTime;
        LastOnWallRightTime  -= Time.deltaTime;
        LastOnWallLeftTime   -= Time.deltaTime;
        LastPressedJumpTime  -= Time.deltaTime;
        LastPressedDashTime  -= Time.deltaTime;

        _moveInput.x = inputConfig != null ? inputConfig.GetHorizontalInput() : Input.GetAxisRaw("Horizontal");
        _moveInput.y = inputConfig != null ? inputConfig.GetVerticalInput()   : Input.GetAxisRaw("Vertical");

        if (isDead) return;

        if (isWallJumping && Time.time - _wallJumpStartTime < Data.wallJumpTime)
        {
            if (_lastWallJumpDir == 1 && _moveInput.x > 0) _moveInput.x = 0; 
            if (_lastWallJumpDir == -1 && _moveInput.x < 0) _moveInput.x = 0; 
        }

        bool isAttackLocked = playerAttack != null && playerAttack.IsAttacking;

        if (isAttackLocked)
        {
            if (this.isGrounded)
            {
                _moveInput.x = 0; // Stop horizontal movement while attacking on the ground
            }
        }
        else if (_moveInput.x != 0)
        {
            CheckDirectionToFace(_moveInput.x > 0);
        }

        bool jumpDown = inputConfig != null ? inputConfig.GetJumpDown() : Input.GetKeyDown(KeyCode.Space);
        bool jumpUp   = inputConfig != null ? inputConfig.GetJumpUp()   : Input.GetKeyUp(KeyCode.Space);

        if (!isAttackLocked && jumpDown)
        {
            if (_moveInput.y < -0.1f)
            {
                StartCoroutine(DropThroughPlatform());
                return;
            }

            if (isClimbing)
            {
                isClimbing = false;
                Jump();
            }
            else
            {
                OnJumpInput();
            }
        }

        if (jumpUp)
            OnJumpUpInput();

        if (!isAttackLocked && (inputConfig != null ? inputConfig.GetDashDown() : Input.GetKeyDown(KeyCode.LeftShift)))
            OnDashInput();

        bool attackDown = inputConfig != null ? inputConfig.GetAttackDown() : Input.GetKeyDown(KeyCode.J);
        if (!isPlunging && LastOnGroundTime <= 0 && _moveInput.y < -0.1f && attackDown
            && GameDataManager.Instance != null && GameDataManager.Instance.isPlungeAttackUnlocked)
        {
            isPlunging            = true;
            isJumping             = false;
            _isJumpFalling        = false;
            rb.linearVelocity     = new Vector2(0f, -plungeSpeed);
            SetGravityScale(0);
            if (_fallDamageHandler != null) _fallDamageHandler.FallDamageModifier = 0.3f;
        }

        bool isGrounded = false;
        if (!isDashing)
        {
            if (Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, _groundLayer))
            {
                if (rb.linearVelocity.y <= 0.05f && (Time.time - _lastJumpExecTime > 0.15f))
                {
                    LastOnGroundTime = Data.coyoteTime;
                    isJumping        = false;
                    isWallJumping    = false;
                    _isJumpFalling   = false;
                }
                isGrounded = true;
            }

            RaycastHit2D fromHit = Physics2D.BoxCast(_fromWallCheckPoint.position, _wallCheckSize, 0f, isFacingRight ? Vector2.right : Vector2.left, 0.1f, _wallLayer);
            bool fromWall = fromHit.collider != null && Mathf.Abs(fromHit.normal.x) > 0.5f;

            RaycastHit2D backHit = Physics2D.BoxCast(_backWallCheckPoint.position, _wallCheckSize, 0f, isFacingRight ? Vector2.left : Vector2.right, 0.1f, _wallLayer);
            bool backWall = backHit.collider != null && Mathf.Abs(backHit.normal.x) > 0.5f;

            if ((fromWall && isFacingRight) || (backWall && !isFacingRight))
                LastOnWallRightTime = Data.coyoteTime;

            if ((fromWall && !isFacingRight) || (backWall && isFacingRight))
                LastOnWallLeftTime = Data.coyoteTime;

            LastOnWallTime = Mathf.Max(LastOnWallRightTime, LastOnWallLeftTime);

            if (!isLedgeGrabbing && _canLedgeGrab && !isGrounded)
            {
                bool chestHitsWall = fromWall;
                bool headClear     = _ledgeCheck != null && !Physics2D.OverlapBox(_ledgeCheck.position, _ledgeCheckSize, 0f, _wallLayer);
                bool isFacingWall = (fromWall && isFacingRight) || (fromWall && !isFacingRight);

                bool isRealLedge = false;
                if (chestHitsWall && headClear && fromHit.collider != null)
                {
                    float directionX = isFacingRight ? 1f : -1f;
                    // Raycast higher up and further in to accommodate colliders
                    Vector2 rayOrigin = new Vector2(fromHit.point.x + (directionX * 0.15f), _ledgeCheck.position.y + 0.5f);

                    RaycastHit2D ledgeTopHit = Physics2D.Raycast(rayOrigin, Vector2.down, 1.0f, _wallLayer);
                    Debug.DrawRay(rayOrigin, Vector2.down * 1.0f, Color.cyan);

                    if (ledgeTopHit.collider != null)
                    {
                        isRealLedge = true;
                    }
                }

                if (chestHitsWall && headClear && isFacingWall && !_ledgeCoroutineRunning && isRealLedge)
                {
                    isClimbing = false;
                    isSliding = false;
                    isWallJumping = false;
                    rb.linearVelocity = Vector2.zero; 
                    
                    StartCoroutine(LedgeClimbCoroutine());
                }
            }
        }

        if (isClimbing)
        {
            // Cancel climbing if moving horizontally away or grounded without holding up
            if (Mathf.Abs(_moveInput.x) > 0.1f && Mathf.Abs(_moveInput.y) < 0.1f)
            {
                isClimbing = false;
            }
            else if (isGrounded && _moveInput.y <= 0)
            {
                isClimbing = false;
            }
        }

        if (_isTouchingClimbable && _moveInput.y != 0 && !isClimbing && !isLedgeGrabbing && !_ledgeCoroutineRunning)
        {
            isClimbing        = true;
            isJumping         = false;
            isWallJumping     = false;
            _isJumpFalling    = false;
            _isJumpCut        = false;
            isPlunging        = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            OnCling?.Invoke(this, EventArgs.Empty);
        }

        if (isJumping && rb.linearVelocity.y < 0)
        {
            isJumping = false;
            if (!isWallJumping) _isJumpFalling = true;
        }

        if (isWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
            isWallJumping = false;

        if (LastOnGroundTime > 0 && !isJumping && !isWallJumping)
        {
            _isJumpCut = false;
            if (!isJumping) _isJumpFalling = false;
        }

        // Trigger OnFall if running off a ledge
        if (!_wasGrounded && !isGrounded && rb.linearVelocity.y < -0.1f && !isJumping && !isWallJumping && !isClimbing && !isLedgeGrabbing && !isSliding && !isDashing && !isPlunging)
        {
            // We use a small hack to ensure OnFall isn't spammed: Only trigger when passing the velocity threshold.
            // A better way is to track if we already sent OnFall.
        }

        int currentMaxJumps = (GameDataManager.Instance != null && GameDataManager.Instance.isTripleJumpUnlocked) ? 3 : 2;
        if (LastOnGroundTime > 0 || isWallJumping || isLedgeGrabbing || isClimbing)
        {
            _jumpsLeft = currentMaxJumps;
            if (_phantomShadowAnchor != null)
            {
                Destroy(_phantomShadowAnchor);
                _phantomShadowAnchor = null;
            }
        }
        else
        {
            if (_jumpsLeft == currentMaxJumps)
            {
                _jumpsLeft = currentMaxJumps - 1;
            }
        }

        if (LastOnGroundTime > 0 || isLedgeGrabbing || isClimbing)
        {
            _lastWallJumpDir = 0;
        }

        if (!isDashing)
        {
            if (CanJump() && LastPressedJumpTime > 0)
            {
                isJumping      = true;
                isWallJumping  = false;
                _isJumpCut     = false;
                _isJumpFalling = false;
                Jump();
            }
            else if (CanWallJump() && LastPressedJumpTime > 0)
            {
                isWallJumping          = true;
                isJumping              = false;
                _isJumpCut             = false;
                _isJumpFalling         = false;
                _wallJumpStartTime     = Time.time;
                
                _lastWallJumpDir       = (LastOnWallRightTime > 0) ? 1 : -1; 
                WallJump(_lastWallJumpDir);
            }
        }

        if (CanDash() && LastPressedDashTime > 0)
        {
            Sleep(Data.dashSleepTime);

            _lastDashDir = (_moveInput == Vector2.zero)
                ? (isFacingRight ? Vector2.right : Vector2.left)
                : _moveInput;

            isDashing      = true;
            isJumping      = false;
            isWallJumping  = false;
            _isJumpCut     = false;
            _isJumpFalling = false;

            OnDash?.Invoke(this, EventArgs.Empty);
            StartCoroutine(nameof(StartDash), _lastDashDir);
        }

        if (CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)))
            isSliding = true;
        else
            isSliding = false;

        if (!_isDashAttacking)
        {
            if (isPlunging)
            {
                SetGravityScale(0); 
            }
            else if (isLedgeGrabbing || isClimbing)
            {
                SetGravityScale(0);
            }
            else if (isSliding)
            {
                SetGravityScale(0); // Tắt trọng lực để đứng im trên tường
            }
            else if (rb.linearVelocity.y < 0 && _moveInput.y < 0)
            {
                SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -Data.maxFastFallSpeed));
            }
            else if (_isJumpCut)
            {
                SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -Data.maxFallSpeed));
            }
            else if ((isJumping || isWallJumping || _isJumpFalling) && Mathf.Abs(rb.linearVelocity.y) < Data.jumpHangTimeThreshold)
            {
                SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
            }
            else if (rb.linearVelocity.y < 0)
            {
                SetGravityScale(Data.gravityScale * Data.fallGravityMult);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -Data.maxFallSpeed));
            }
            else
            {
                SetGravityScale(Data.gravityScale);
            }
        }
        else
        {
            SetGravityScale(0);
        }

        bool isCurrentlyGrounded = Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, _groundLayer);

        if (!isCurrentlyGrounded && _wasGrounded && rb.linearVelocity.y < 0.1f && !isJumping && !isDashing)
        {
            // Player just walked off a ledge
            OnFall?.Invoke(this, EventArgs.Empty);
        }

        if (isCurrentlyGrounded && !_wasGrounded)
        {
            if (_fallDamageHandler != null) _fallDamageHandler.FallDamageModifier = 1f;
            if (isPlunging)
            {
                isPlunging = false;
                SetGravityScale(Data.gravityScale);
                playerAttack?.ExecutePlungeAOE();
            }
            OnLand?.Invoke(this, EventArgs.Empty);
        }

        _wasGrounded = isCurrentlyGrounded;
        bool isMoving = Mathf.Abs(_moveInput.x) > 0.1f;
        if (isCurrentlyGrounded && isMoving  && !_wasMoving) OnRun?.Invoke(this, EventArgs.Empty);
        if (isCurrentlyGrounded && !isMoving && _wasMoving)  OnStopRun?.Invoke(this, EventArgs.Empty);
        _wasMoving = isMoving;
    }

    private void FixedUpdate()
    {
        if (isClimbing)
        {
            rb.linearVelocity = new Vector2(0f, _moveInput.y * climbSpeed);
            return;
        }

        if (isLedgeGrabbing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isPlunging)
        {
            rb.linearVelocity = new Vector2(0f, -plungeSpeed);
            return;
        }

        if (isSliding)
        {
            rb.linearVelocity = new Vector2(0f, -wallSlideSpeed);
            return;
        }

        if (!isDashing)
        {
            if (isWallJumping)
                Run(Data.wallJumpRunLerp);
            else
                Run(1f);
        }
        else if (_isDashAttacking)
        {
            Run(Data.dashEndRunLerp);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Climbable"))
            _isTouchingClimbable = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Climbable")) return;
        _isTouchingClimbable = false;
        isClimbing           = false;
    }

    // ------------------------------------------------------------------ //
    //  Public Input Handlers                                               //
    // ------------------------------------------------------------------ //

    public void OnJumpInput()
    {
        LastPressedJumpTime = Data.jumpInputBufferTime;
    }

    public void OnJumpUpInput()
    {
        if (CanJumpCut() || CanWallJumpCut())
            _isJumpCut = true;
    }

    public void OnDashInput()
    {
        LastPressedDashTime = Data.dashInputBufferTime;
    }

    public void SetGravityScale(float scale)
    {
        rb.gravityScale = scale;
    }

    public void SetWaterSpeedMultiplier(float multiplier)
    {
        _waterSpeedMultiplier = multiplier;
    }

    public void SetLinearDrag(float drag)
    {
        rb.linearDamping = drag;
    }

    public void ResetLinearDrag()
    {
        rb.linearDamping = _defaultLinearDrag;
    }

    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != isFacingRight) Turn();
    }

    // ------------------------------------------------------------------ //
    //  Private Movement Helpers                                            //
    // ------------------------------------------------------------------ //

    private void Sleep(float duration)
    {
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private void Run(float lerpAmount)
    {
        if (playerCombat.IsKnockedBack) return;

        float targetSpeed = _moveInput.x * Data.runMaxSpeed * _waterSpeedMultiplier;
        targetSpeed = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, lerpAmount);

        float accelRate;
        if (LastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.runAccelInAirMultiplier : Data.runDeccelAmount * Data.runDeccelInAirMultiplier;

        if ((isJumping || isWallJumping || _isJumpFalling) && Mathf.Abs(rb.linearVelocity.y) < Data.jumpHangTimeThreshold)
        {
            accelRate   *= Data.jumpHangAccelMultiplier;
            targetSpeed *= Data.jumpHangMaxSpeedMultiplier;
        }

        if (Data.doConserveMomentum &&
            Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetSpeed) &&
            Mathf.Abs(targetSpeed) > 0.01f)
        {
            accelRate = 0;
        }

        float speedDif  = targetSpeed - rb.linearVelocity.x;
        float movement  = speedDif * accelRate;
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    private void Turn()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        isFacingRight = !isFacingRight;
    }

    private void Jump()
    {
        _lastJumpExecTime   = Time.time;
        LastPressedJumpTime = 0;
        LastOnGroundTime    = 0;

        if (inventoryManager != null && inventoryManager.HasRelic("Phantom_Shadow"))
        {
            if (_phantomShadowAnchor != null)
            {
                transform.position = _phantomShadowAnchor.transform.position;
                rb.linearVelocity = Vector2.zero;
                Destroy(_phantomShadowAnchor);
                _phantomShadowAnchor = null;
                _jumpsLeft = 0;
                OnJump?.Invoke(this, EventArgs.Empty);
                return;
            }
            else if (_jumpsLeft == 1) 
            {
                _phantomShadowAnchor = phantomShadowPrefab != null 
                    ? Instantiate(phantomShadowPrefab, transform.position, Quaternion.identity)
                    : new GameObject("PhantomAnchor") { transform = { position = transform.position } };
                _jumpsLeft = 2; 
            }
        }

        _jumpsLeft--;

        if (rb.linearVelocity.y < 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        rb.AddForce(Vector2.up * Data.jumpForce, ForceMode2D.Impulse);
        OnJump?.Invoke(this, EventArgs.Empty);
    }

    private void WallJump(int dir)
    {
        _lastJumpExecTime   = Time.time;
        LastPressedJumpTime = 0;
        LastOnGroundTime    = 0;
        LastOnWallRightTime = 0;
        LastOnWallLeftTime  = 0;

        Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
        
        // Vì "dir" bây giờ là hướng của tường (1 hoặc -1), nên "-dir" sẽ văng ngược lại bức tường. 
        force.x *= -dir; 

        if (Mathf.Sign(rb.linearVelocity.x) != Mathf.Sign(force.x))
            force.x -= rb.linearVelocity.x;

        if (rb.linearVelocity.y < 0)
            force.y -= rb.linearVelocity.y;

        rb.AddForce(force, ForceMode2D.Impulse);

        CheckDirectionToFace(dir != 1); 
    }

    // ------------------------------------------------------------------ //
    //  Can-Do Predicates                                                   //
    // ------------------------------------------------------------------ //

    private bool CanJump()      => _jumpsLeft > 0 && !isWallJumping && !isLedgeGrabbing && (Time.time - _lastJumpExecTime > 0.15f);

    private bool CanWallJump()
    {
        if (LastPressedJumpTime <= 0 || LastOnWallTime <= 0 || LastOnGroundTime > 0 || isLedgeGrabbing || (Time.time - _lastJumpExecTime <= 0.15f))
            return false;

        int currentWallDir = (LastOnWallRightTime > 0) ? 1 : -1;
        
        if (currentWallDir == _lastWallJumpDir)
            return false;

        return true;
    }
    private bool CanJumpCut()     => isJumping     && rb.linearVelocity.y > 0;
    private bool CanWallJumpCut() => isWallJumping && rb.linearVelocity.y > 0;

    private bool CanDash()
    {
        if (!isDashing && _dashesLeft < Data.dashAmount && LastOnGroundTime > 0 && !_dashRefilling)
            StartCoroutine(nameof(RefillDash), 1);
        return _dashesLeft > 0;
    }

    public bool CanSlide() => LastOnWallTime > 0 && !isWallJumping && LastOnGroundTime <= 0 && !isLedgeGrabbing && !_ledgeCoroutineRunning;
    
    // ------------------------------------------------------------------ //
    //  Coroutines                                                          //
    // ------------------------------------------------------------------ //

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }

    private IEnumerator StartDash(Vector2 dir)
    {
        LastOnGroundTime    = 0;
        LastPressedDashTime = 0;

        float startTime = Time.time;
        _dashesLeft--;
        _isDashAttacking = true;

        SetGravityScale(0);

        if (healthSystem != null) healthSystem.SetInvincible(true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        bool hasSpaceClaw = inventoryManager != null && inventoryManager.HasRelic("Space_Claw");
        if (hasSpaceClaw && _moveInput.y > 0.1f)
        {
            dir = new Vector2(isFacingRight ? 1 : -1, 1).normalized;
        }

        while (Time.time - startTime <= Data.dashAttackTime)
        {
            rb.linearVelocity = dir.normalized * Data.dashSpeed;
            
            if (hasSpaceClaw)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, spaceClawHitRadius, LayerMask.GetMask("Enemy"));
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<IDamageable>(out var damageable) && !damageable.IsDead)
                    {
                        DamageInfo knockup = new DamageInfo 
                        { 
                            knockbackDirection = Vector2.up, 
                            knockbackForce = spaceClawKnockupForce,
                            baseDamage = 5,
                            attacker = gameObject
                        };
                        damageable.TakeDamage(knockup);
                    }
                }
            }
            
            yield return null;
        }

        startTime        = Time.time;
        _isDashAttacking = false;

        if (healthSystem != null) healthSystem.SetInvincible(false);

        SetGravityScale(Data.gravityScale);
        rb.linearVelocity = dir.normalized * Data.dashEndSpeed;

        while (Time.time - startTime <= Data.dashEndTime)
            yield return null;

        isDashing = false;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
    }

    private IEnumerator RefillDash(int amount)
    {
        _dashRefilling = true;
        yield return new WaitForSeconds(Data.dashRefillTime);
        _dashRefilling = false;
        _dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
    }

    private IEnumerator LedgeClimbCoroutine()
    {
        _ledgeCoroutineRunning = true;
        isLedgeGrabbing = true;
        _canLedgeGrab = false;
        rb.linearVelocity = Vector2.zero;
        SetGravityScale(0);

        OnGrab?.Invoke(this, EventArgs.Empty);

        // Wait for horizontal input towards the ledge to trigger get up
        float directionX = isFacingRight ? 1f : -1f;
        while (Mathf.Sign(_moveInput.x) != Mathf.Sign(directionX) || Mathf.Abs(_moveInput.x) < 0.1f)
        {
            // if player presses down or away, we could cancel the ledge grab, but for now just wait.
            // Alternatively, pressing down cancels it:
            if (_moveInput.y < -0.1f || (Mathf.Sign(_moveInput.x) != Mathf.Sign(directionX) && Mathf.Abs(_moveInput.x) > 0.1f))
            {
                isLedgeGrabbing = false;
                SetGravityScale(Data.gravityScale);
                _ledgeCoroutineRunning = false;
                yield return new WaitForSeconds(0.15f);
                _canLedgeGrab = true;
                yield break; // Exit coroutine
            }
            yield return null;
        }

        OnGetup?.Invoke(this, EventArgs.Empty);
        yield return new WaitForSeconds(ledgeGrabDuration);

        float xDir = isFacingRight ? _ledgeClimbXOffset : -_ledgeClimbXOffset;
        rb.MovePosition(rb.position + new Vector2(xDir, _ledgeClimbYOffset));

        SetGravityScale(Data.gravityScale);
        isLedgeGrabbing = false;

        yield return new WaitForSeconds(0.15f);
        _canLedgeGrab = true;
        _ledgeCoroutineRunning = false;
    }

    private IEnumerator DropThroughPlatform()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(_groundCheck.position, _groundCheckSize, 0f, _groundLayer);        
        foreach (Collider2D col in colliders)
        {
            PlatformEffector2D effector = col.GetComponent<PlatformEffector2D>();
            if (effector == null && col.transform.parent != null)
                effector = col.transform.parent.GetComponent<PlatformEffector2D>();

            if (effector != null && effector.useOneWay && playerCollider != null)
            {
                Physics2D.IgnoreCollision(playerCollider, col, true);
                
                yield return new WaitForSeconds(dropTime);
                
                Physics2D.IgnoreCollision(playerCollider, col, false);
                break;
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  Event Handlers & Gizmos                                            //
    // ------------------------------------------------------------------ //

    private void PlayerInteract_OnDead(object sender, EventArgs e)
    {
        isDead            = true;
        rb.linearVelocity = Vector2.zero;
        this.enabled      = false;
    }

    private void OnDestroy()
    {
        PlayerInteract.Instance.OnDead -= PlayerInteract_OnDead;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundCheck.position, _groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(_fromWallCheckPoint.position, _wallCheckSize);
        Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);

        if (_ledgeCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(_ledgeCheck.position, _ledgeCheckSize);
        }
    }
}