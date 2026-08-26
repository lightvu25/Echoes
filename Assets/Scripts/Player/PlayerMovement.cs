using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    private static readonly WaitForFixedUpdate WaitForPhysicsStep = new WaitForFixedUpdate();

    public event EventHandler OnIdle;
    public event EventHandler OnJump;
    public event EventHandler OnLand;
    public event EventHandler OnRun;
    public event EventHandler OnDash;
    public event EventHandler OnWallJump;
    public event Action<int> OnJumpPerformed;
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
    private EntityAudioManager audioManager;

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
    private readonly System.Collections.Generic.HashSet<object> tripleJumpSources = new System.Collections.Generic.HashSet<object>();
    private float _lastDownInputTime;
    private bool  _wasDownInput;
    private float _plungeStartY;
    
    private float stunTimer;
    public bool isStunned => stunTimer > 0f;

    public void ApplyStun(float duration)
    {
        stunTimer = Mathf.Max(stunTimer, duration);
    }
    
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
    private float _buffSpeedMultiplier = 1f;
    private float _defaultLinearDrag;

    private bool _isTouchingClimbable;

    private bool _canLedgeGrab = true;
    private bool _ledgeCoroutineRunning = false;
    private bool _ledgeTransitioning;
    private int _activeWallDirection;
    private int _ledgeWallDirection;
    private float _ledgeWallSurfaceX;
    private float _ledgeTopY;
    private readonly RaycastHit2D[] _wallCastHits = new RaycastHit2D[4];
    private ContactFilter2D _wallContactFilter;

    public bool isGrounded => LastOnGroundTime > 0;
    public bool isRunning => Mathf.Abs(rb.linearVelocity.x) > 0.1f && isGrounded;
    public Vector2 LastDashDirection => _lastDashDir;
    public bool IsJumpHeld => inputConfig != null ? inputConfig.GetJumpHeld() : Input.GetKey(KeyCode.Space);
    public int CurrentMaxJumps => (GameDataManager.Instance != null && GameDataManager.Instance.isTripleJumpUnlocked) || tripleJumpSources.Count > 0 ? 3 : 2;

    private bool _isDroppingThroughPlatform;

    [Header("Checks")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private Vector2   _groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)]
    [SerializeField] private Transform _fromWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2   _wallCheckSize = new Vector2(0.5f, 1f);

    [Header("Wall Contact")]
    [Tooltip("Small separation retained between the player collider and wall to keep Physics2D contacts stable.")]
    [SerializeField, Min(0.001f)] private float wallContactSkin = 0.01f;
    [Tooltip("Maximum collider-to-wall distance at which wall slide may begin.")]
    [SerializeField, Min(0.01f)] private float wallContactProbeDistance = 0.04f;
    [Tooltip("Gentle inward speed that keeps the player attached while sliding.")]
    [SerializeField, Min(0f)] private float wallStickSpeed = 1f;

    [Header("Ledge Climb")]
    [SerializeField] private Transform _ledgeCheck;
    [SerializeField] private Vector2   _ledgeCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private float     _ledgeClimbXOffset = 0.5f;
    [SerializeField] private float     _ledgeClimbYOffset = 1f;
    [Tooltip("How far the player collider may search for the wall when beginning a ledge grab.")]
    [SerializeField, Min(0.05f)] private float ledgeWallProbeDistance = 0.25f;
    [Tooltip("How far the collider top overlaps above the ledge while hanging.")]
    [SerializeField, Min(0f)] private float ledgeHangTopOverlap = 0.15f;
    [FormerlySerializedAs("ledgeGrabDuration")]
    [SerializeField, Min(0.05f)] private float ledgeClimbDuration = 0.35f;
    [Tooltip("Moves the body upward first so its collider clears the ledge before moving onto it.")]
    [SerializeField] private AnimationCurve ledgeClimbVerticalCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.15f, 0.02f),
        new Keyframe(0.72f, 0.9f),
        new Keyframe(1f, 1f));
    [Tooltip("Delays most horizontal movement until the body is above the ledge.")]
    [SerializeField] private AnimationCurve ledgeClimbHorizontalCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 0.04f),
        new Keyframe(0.82f, 0.75f),
        new Keyframe(1f, 1f));

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
        audioManager = GetComponentInChildren<EntityAudioManager>();
        _defaultLinearDrag = rb.linearDamping;

        _wallContactFilter = new ContactFilter2D();
        _wallContactFilter.SetLayerMask(_wallLayer);
        _wallContactFilter.useTriggers = false;
    }

    private void Start()
    {
        SetGravityScale(Data.gravityScale);
        isFacingRight = transform.localScale.x > 0;
        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.OnDead += PlayerInteract_OnDead;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        LastOnGroundTime     -= Time.deltaTime;
        LastOnWallTime       -= Time.deltaTime;
        LastOnWallRightTime  -= Time.deltaTime;
        LastOnWallLeftTime   -= Time.deltaTime;
        LastPressedJumpTime  -= Time.deltaTime;
        LastPressedDashTime  -= Time.deltaTime;

        if (isDead) return;

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            _moveInput = Vector2.zero;
            LastPressedJumpTime = 0f;
            LastPressedDashTime = 0f;
        }
        else
        {
            _moveInput.x = inputConfig != null ? inputConfig.GetHorizontalInput() : Input.GetAxisRaw("Horizontal");
            _moveInput.y = inputConfig != null ? inputConfig.GetVerticalInput()   : Input.GetAxisRaw("Vertical");

            bool isDownInput = _moveInput.y < -0.1f;
            if (isDownInput && !_wasDownInput)
            {
                if (Time.time - _lastDownInputTime < 0.3f)
                {
                    StartCoroutine(DropThroughPlatform());
                }
                _lastDownInputTime = Time.time;
            }
            _wasDownInput = isDownInput;
        }

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
                _moveInput.x = 0;
            }
        }
        else if (_moveInput.x != 0)
        {
            CheckDirectionToFace(_moveInput.x > 0);
        }

        bool jumpDown = inputConfig != null ? inputConfig.GetJumpDown() : Input.GetKeyDown(KeyCode.Space);
        bool jumpUp   = inputConfig != null ? inputConfig.GetJumpUp()   : Input.GetKeyUp(KeyCode.Space);

        if (jumpDown)
        {
            if (isAttackLocked && playerAttack != null)
            {
                playerAttack.CancelAttackForMovement();
                isAttackLocked = false;
            }

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

        if (inputConfig != null ? inputConfig.GetDashDown() : Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (isAttackLocked && playerAttack != null)
            {
                playerAttack.CancelAttackForMovement();
                isAttackLocked = false;
            }
            OnDashInput();
        }

        bool attackDown = inputConfig != null ? inputConfig.GetAttackMeleeDown() : Input.GetKeyDown(KeyCode.J);
        
        if (!isPlunging && attackDown && _moveInput.y < -0.1f && LastOnGroundTime <= 0)
        {
            isPlunging            = true;
            isJumping             = false;
            _isJumpFalling        = false;
            rb.linearVelocity     = new Vector2(0f, -plungeSpeed);
            SetGravityScale(0);
            if (_fallDamageHandler != null) _fallDamageHandler.BypassNextFallDamage = true;
            _plungeStartY = transform.position.y;
            
            if (playerAttack != null) playerAttack.StartPlungeFallHitbox();
        }

        bool isGrounded = false;
        if (!isDashing && !_isDroppingThroughPlatform)
        {
            if (Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, _groundLayer))
            {
                if (rb.linearVelocity.y <= 0.05f && (Time.time - _lastJumpExecTime > 0.15f))
                {
                    if (!isPlunging || Mathf.Abs(rb.linearVelocity.y) < 0.1f)
                    {
                        LastOnGroundTime = Data.coyoteTime;
                        isJumping        = false;
                        isWallJumping    = false;
                        _isJumpFalling   = false;
                        isGrounded       = true;
                    }
                }
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

            if (!isLedgeGrabbing && _canLedgeGrab && !isGrounded && !isDashing)
            {
                CheckLedgeGrab(fromWall, fromHit);
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

        int currentMaxJumps = CurrentMaxJumps;
        if (LastOnGroundTime > 0 || isWallJumping || isLedgeGrabbing || isClimbing || LastOnWallTime > 0)
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

        int requestedWallDirection = _moveInput.x > 0.1f ? 1 : _moveInput.x < -0.1f ? -1 : 0;
        bool hasWallContact = requestedWallDirection != 0
            && TryGetWallHit(requestedWallDirection, wallContactProbeDistance, out _);
        bool inputTargetsDetectedWall = (requestedWallDirection < 0 && LastOnWallLeftTime > 0)
            || (requestedWallDirection > 0 && LastOnWallRightTime > 0);

        isSliding = CanSlide() && hasWallContact && inputTargetsDetectedWall;
        _activeWallDirection = isSliding ? requestedWallDirection : 0;

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

        bool isCurrentlyGrounded = !_isDroppingThroughPlatform && Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, _groundLayer);

        if (!isCurrentlyGrounded && _wasGrounded && rb.linearVelocity.y < 0.1f && !isJumping && !isDashing)
        {
            // Player just walked off a ledge
            OnFall?.Invoke(this, EventArgs.Empty);
        }

        if (isCurrentlyGrounded && !_wasGrounded)
        {
            if (isPlunging)
            {
                if (_fallDamageHandler != null) _fallDamageHandler.BypassNextFallDamage = true;
                isPlunging = false;
                SetGravityScale(Data.gravityScale);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                float dropDistance = _plungeStartY - transform.position.y;
                
                if (playerAttack != null) 
                {
                    playerAttack.StopPlungeFallHitbox();
                    playerAttack.ExecutePlungeAOE(dropDistance);
                }
            }
            OnLand?.Invoke(this, EventArgs.Empty);
        }

        _wasGrounded = isCurrentlyGrounded;
        bool isMoving = Mathf.Abs(_moveInput.x) > 0.1f;
        if (isCurrentlyGrounded && isMoving  && !_wasMoving) OnRun?.Invoke(this, EventArgs.Empty);
        if (isCurrentlyGrounded && !isMoving && _wasMoving)  OnStopRun?.Invoke(this, EventArgs.Empty);
        _wasMoving = isMoving;

        if (audioManager != null)
        {
            if (isCurrentlyGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            {
                audioManager.PlayLoopingSound("Run");
            }
            else
            {
                audioManager.StopLoopingSound();
            }
        }
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
            if (!_ledgeTransitioning)
            {
                MaintainLedgeHangPosition();
            }

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
            MaintainWallSlideContact();
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

    public void SetBuffSpeedMultiplier(float multiplier)
    {
        _buffSpeedMultiplier = multiplier;
    }

    public void SetTripleJump(object source, bool enabled)
    {
        if (source == null) return;
        if (enabled) tripleJumpSources.Add(source);
        else tripleJumpSources.Remove(source);
    }

    public void ApplyRelicBounce(float upwardVelocity)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, upwardVelocity));
    }

    public void PullToward(Vector3 targetPosition, float speed)
    {
        Vector2 direction = ((Vector2)targetPosition - rb.position).normalized;
        rb.linearVelocity = direction * Mathf.Max(0f, speed);
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

        float targetSpeed = _moveInput.x * Data.runMaxSpeed * _waterSpeedMultiplier * _buffSpeedMultiplier;
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
        int jumpOrdinal = Mathf.Clamp(CurrentMaxJumps - _jumpsLeft + 1, 1, CurrentMaxJumps);
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
                OnJumpPerformed?.Invoke(jumpOrdinal);
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
        OnJumpPerformed?.Invoke(jumpOrdinal);
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
        OnWallJump?.Invoke(this, EventArgs.Empty);

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
        return !isDashing && _dashesLeft > 0;
    }

    public bool CanSlide() => LastOnWallTime > 0 && !isWallJumping && LastOnGroundTime <= 0 && !isLedgeGrabbing && !_ledgeCoroutineRunning;
    
    // ------------------------------------------------------------------ //
    //  Coroutines                                                          //
    // ------------------------------------------------------------------ //

    private IEnumerator PerformSleep(float duration)
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.PauseTime("PlayerDash");
            yield return new WaitForSecondsRealtime(duration);
            TimeManager.Instance.ResumeTime("PlayerDash");
        }
        else
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            
            // Do not force time back to 1 if a UI panel is open
            if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen)
            {
                yield break;
            }
            
            Time.timeScale = 1f;
        }
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
    }

    private IEnumerator RefillDash(int amount)
    {
        _dashRefilling = true;
        yield return new WaitForSeconds(Data.dashRefillTime);
        _dashRefilling = false;
        _dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
    }

    private void CheckLedgeGrab(bool fromWall, RaycastHit2D fromHit)
    {
        bool chestHitsWall = fromWall;
        bool headClear     = _ledgeCheck != null && !Physics2D.OverlapBox(_ledgeCheck.position, _ledgeCheckSize, 0f, _wallLayer);
        int directionX = isFacingRight ? 1 : -1;
        bool hasColliderWall = TryGetWallHit(directionX, ledgeWallProbeDistance, out RaycastHit2D colliderWallHit);

        if (chestHitsWall && headClear && hasColliderWall && !_ledgeCoroutineRunning && fromHit.collider != null)
        {
            float wallSurfaceX = colliderWallHit.point.x;
            Vector2 rayOrigin = new Vector2(wallSurfaceX + (directionX * 0.15f), _ledgeCheck.position.y + 0.5f);

            RaycastHit2D ledgeTopHit = Physics2D.Raycast(rayOrigin, Vector2.down, 1.0f, _wallLayer);

            if (ledgeTopHit.collider != null)
            {
                _ledgeWallDirection = directionX;
                _ledgeWallSurfaceX = wallSurfaceX;
                _ledgeTopY = ledgeTopHit.point.y;

                Vector2 targetHangPos = playerCollider != null
                    ? PlayerWallGeometry.CalculateHangPosition(
                        rb.position,
                        playerCollider.bounds,
                        _ledgeWallSurfaceX,
                        _ledgeTopY,
                        _ledgeWallDirection,
                        wallContactSkin,
                        ledgeHangTopOverlap)
                    : new Vector2(transform.position.x, _ledgeTopY - 0.55f);

                StartLedgeGrab(targetHangPos);
            }
        }
    }

    private void StartLedgeGrab(Vector2 targetHangPos)
    {
        isClimbing = false;
        isSliding = false;
        isWallJumping = false;
        isPlunging = false;
        
        isLedgeGrabbing = true;
        _canLedgeGrab = false;
        _ledgeCoroutineRunning = true;
        _ledgeTransitioning = true;
        
        rb.linearVelocity = Vector2.zero;
        SetGravityScale(0);
        
        OnGrab?.Invoke(this, EventArgs.Empty);

        StartCoroutine(SmoothMoveToHang(targetHangPos));
    }

    private IEnumerator SmoothMoveToHang(Vector2 targetPos)
    {
        const float duration = 0.08f;
        float timer = 0f;
        Vector2 startPos = rb.position;

        while (timer < duration)
        {
            yield return WaitForPhysicsStep;

            timer = Mathf.Min(timer + Time.fixedDeltaTime, duration);
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            rb.MovePosition(Vector2.LerpUnclamped(startPos, targetPos, t));
        }

        yield return WaitForPhysicsStep;
        rb.position = targetPos;
        rb.linearVelocity = Vector2.zero;
        _ledgeTransitioning = false;
        StartCoroutine(WaitForLedgeInput());
    }

    private IEnumerator WaitForLedgeInput()
    {
        float directionX = _ledgeWallDirection != 0
            ? _ledgeWallDirection
            : (isFacingRight ? 1f : -1f);
        
        while (isLedgeGrabbing)
        {
            rb.linearVelocity = Vector2.zero;
            
            if (Mathf.Sign(_moveInput.x) == Mathf.Sign(directionX) && Mathf.Abs(_moveInput.x) > 0.1f)
            {
                BeginClimb();
                yield break;
            }
            
            if (_moveInput.y < -0.1f || (Mathf.Sign(_moveInput.x) != Mathf.Sign(directionX) && Mathf.Abs(_moveInput.x) > 0.1f))
            {
                ExitLedgeGrab();
                yield break;
            }
            
            yield return null;
        }
    }

    private void BeginClimb()
    {
        OnGetup?.Invoke(this, EventArgs.Empty);

        _ledgeTransitioning = true;
        int directionX = _ledgeWallDirection != 0
            ? _ledgeWallDirection
            : (isFacingRight ? 1 : -1);
        Vector2 targetClimbPos = playerCollider != null && _ledgeWallDirection != 0
            ? PlayerWallGeometry.CalculateClimbPosition(
                rb.position,
                playerCollider.bounds,
                _ledgeWallSurfaceX,
                _ledgeTopY,
                directionX,
                wallContactSkin)
            : rb.position + new Vector2(directionX * _ledgeClimbXOffset, _ledgeClimbYOffset);

        StartCoroutine(SmoothClimb(targetClimbPos));
    }

    private IEnumerator SmoothClimb(Vector2 targetPos)
    {
        float duration = Mathf.Max(0.05f, ledgeClimbDuration);
        float timer = 0f;
        Vector2 startPos = rb.position;

        while (timer < duration)
        {
            yield return WaitForPhysicsStep;

            timer = Mathf.Min(timer + Time.fixedDeltaTime, duration);
            float normalizedTime = timer / duration;
            float horizontalProgress = EvaluateLedgeCurve(ledgeClimbHorizontalCurve, normalizedTime);
            float verticalProgress = EvaluateLedgeCurve(ledgeClimbVerticalCurve, normalizedTime);

            Vector2 nextPosition = new Vector2(
                Mathf.LerpUnclamped(startPos.x, targetPos.x, horizontalProgress),
                Mathf.LerpUnclamped(startPos.y, targetPos.y, verticalProgress));

            rb.MovePosition(nextPosition);
        }

        yield return WaitForPhysicsStep;
        rb.position = targetPos;
        rb.linearVelocity = Vector2.zero;
        FinishClimb();
    }

    private static float EvaluateLedgeCurve(AnimationCurve curve, float normalizedTime)
    {
        if (curve == null || curve.length == 0)
        {
            return Mathf.SmoothStep(0f, 1f, normalizedTime);
        }

        return Mathf.Clamp01(curve.Evaluate(normalizedTime));
    }

    private void FinishClimb()
    {
        ExitLedgeGrab();
    }

    private void ExitLedgeGrab()
    {
        isLedgeGrabbing = false;
        _ledgeTransitioning = false;
        _ledgeWallDirection = 0;
        SetGravityScale(Data.gravityScale);
        StartCoroutine(ResetLedgeGrabCooldown());
    }

    private bool TryGetWallHit(int direction, float maxDistance, out RaycastHit2D closestHit)
    {
        closestHit = default;
        if (playerCollider == null || direction == 0)
        {
            return false;
        }

        Vector2 castDirection = Vector2.right * direction;
        int hitCount = playerCollider.Cast(
            castDirection,
            _wallContactFilter,
            _wallCastHits,
            Mathf.Max(0.001f, maxDistance));
        float closestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _wallCastHits[i];
            if (hit.collider == null || Vector2.Dot(hit.normal, castDirection) > -0.5f)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }

        return closestHit.collider != null;
    }

    private void MaintainWallSlideContact()
    {
        int direction = _activeWallDirection != 0
            ? _activeWallDirection
            : (_moveInput.x >= 0f ? 1 : -1);

        if (TryGetWallHit(direction, wallContactProbeDistance, out RaycastHit2D wallHit))
        {
            float correction = Mathf.Max(0f, wallHit.distance - wallContactSkin);
            if (correction > 0f)
            {
                rb.position += Vector2.right * (direction * correction);
            }

            rb.linearVelocity = new Vector2(direction * wallStickSpeed, -wallSlideSpeed);
            return;
        }

        isSliding = false;
        _activeWallDirection = 0;
    }

    private void MaintainLedgeHangPosition()
    {
        if (playerCollider == null || _ledgeWallDirection == 0)
        {
            return;
        }

        Vector2 alignedPosition = PlayerWallGeometry.CalculateHangPosition(
            rb.position,
            playerCollider.bounds,
            _ledgeWallSurfaceX,
            _ledgeTopY,
            _ledgeWallDirection,
            wallContactSkin,
            ledgeHangTopOverlap);
        rb.position = alignedPosition;
    }

    private IEnumerator ResetLedgeGrabCooldown()
    {
        yield return new WaitForSeconds(0.15f);
        _canLedgeGrab = true;
        _ledgeCoroutineRunning = false;
    }

    private IEnumerator DropThroughPlatform()
    {
        LastOnGroundTime = 0f;
        Collider2D[] colliders = Physics2D.OverlapBoxAll(_groundCheck.position, _groundCheckSize, 0f, _groundLayer);        
        foreach (Collider2D col in colliders)
        {
            PlatformEffector2D effector = col.GetComponent<PlatformEffector2D>();
            if (effector == null && col.transform.parent != null)
                effector = col.transform.parent.GetComponent<PlatformEffector2D>();

            if (effector != null && effector.useOneWay && playerCollider != null)
            {
                _isDroppingThroughPlatform = true;
                Physics2D.IgnoreCollision(playerCollider, col, true);
                
                yield return new WaitForSeconds(dropTime);
                
                Physics2D.IgnoreCollision(playerCollider, col, false);
                _isDroppingThroughPlatform = false;
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
        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.OnDead -= PlayerInteract_OnDead;
        }
    }

    private void OnDisable()
    {
        isDashing = false;
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

public static class PlayerWallGeometry
{
    public static Vector2 CalculateHangPosition(
        Vector2 bodyPosition,
        Bounds colliderBounds,
        float wallSurfaceX,
        float ledgeTopY,
        int wallDirection,
        float contactSkin,
        float topOverlap)
    {
        int direction = wallDirection >= 0 ? 1 : -1;
        float skin = Mathf.Max(0f, contactSkin);
        float overlap = Mathf.Max(0f, topOverlap);
        float facingExtent = direction > 0
            ? colliderBounds.max.x - bodyPosition.x
            : bodyPosition.x - colliderBounds.min.x;
        float topExtent = colliderBounds.max.y - bodyPosition.y;

        return new Vector2(
            wallSurfaceX - direction * (facingExtent + skin),
            ledgeTopY - topExtent + overlap);
    }

    public static Vector2 CalculateClimbPosition(
        Vector2 bodyPosition,
        Bounds colliderBounds,
        float wallSurfaceX,
        float ledgeTopY,
        int wallDirection,
        float contactSkin)
    {
        int direction = wallDirection >= 0 ? 1 : -1;
        float skin = Mathf.Max(0f, contactSkin);
        float trailingExtent = direction > 0
            ? bodyPosition.x - colliderBounds.min.x
            : colliderBounds.max.x - bodyPosition.x;
        float bottomExtent = bodyPosition.y - colliderBounds.min.y;

        return new Vector2(
            wallSurfaceX + direction * (trailingExtent + skin),
            ledgeTopY + bottomExtent + skin);
    }
}
