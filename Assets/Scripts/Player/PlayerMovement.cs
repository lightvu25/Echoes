using System;
using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public event EventHandler OnJump;
    public event EventHandler OnLand;
    public event EventHandler OnWalk;
    public event EventHandler OnDash;
    public event EventHandler OnStopWalk;
    public PlayerData Data;

    public Rigidbody2D rb { get; private set; }

    // Variables to track player state
    public bool isFacingRight { get; private set; }
    public bool isJumping { get; private set; }
    public bool isWallJumping { get; private set; }
    public bool isDashing { get; private set; }
    public bool isSliding { get; private set; }

    public float LastOnGroundTime { get; private set; }
    public float LastOnWallTime { get; private set; }
    public float LastOnWallRightTime { get; private set; }
    public float LastOnWallLeftTime { get; private set; }
    public float LastDashTime { get; private set; }

    private bool _isJumpCut;
    private bool _isJumpFalling;

    private float _wallJumpStartTime;
    private int _lastWallJumpDir;

    private int _dashesLeft;
    private bool _dashRefilling;
    private Vector2 _lastDashDir;
    private bool _isDashAttacking;

    private Vector2 _moveInput;
    public float LastPressedJumpTime { get; private set; }
    public float LastPressedDashTime { get; private set; }

    private bool _wasGrounded;
    private bool _wasMoving;

    private bool isDead = false;

    [Header("Checks")]
    [SerializeField] private Transform _groundCheck;

    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
    [Space(5)]
    [SerializeField] private Transform _fromWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);

    [Header("Layers & Tag")]
    [SerializeField] private LayerMask _groundLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        SetGravityScale(Data.gravityScale);
        isFacingRight = true;

        PlayerInteract.Instance.OnDead += PlayerInteract_OnDead;
    }

    private void Update()
    {
        LastOnGroundTime -= Time.deltaTime;
        LastOnWallTime -= Time.deltaTime;
        LastOnWallRightTime -= Time.deltaTime;
        LastOnWallLeftTime -= Time.deltaTime;

        LastPressedJumpTime -= Time.deltaTime;
        LastPressedDashTime -= Time.deltaTime;

        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");

        if (isDead) return;

        if (_moveInput.x != 0)
            CheckDirectionToFace(_moveInput.x > 0);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnJumpInput();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            OnJumpUpInput();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            OnDashInput();
        }

        // Collision Checks
        if (!isDashing && !isJumping)
        {
            // Ground Check
            if (Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, _groundLayer))
            {
                LastOnGroundTime = Data.coyoteTime; // Reset coyote time when grounded
            }
            
            if (((Physics2D.OverlapBox(_fromWallCheckPoint.position, _wallCheckSize, 0f, _groundLayer) && isFacingRight) ||
                 (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0f, _groundLayer) && !isFacingRight)))
            {
                LastOnWallRightTime = Data.coyoteTime; // Reset wall coyote time when touching a wall
            }

            if (((Physics2D.OverlapBox(_fromWallCheckPoint.position, _wallCheckSize, 0f, _groundLayer) && !isFacingRight) ||
                 (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0f, _groundLayer) && isFacingRight)))
            {
                LastOnWallLeftTime = Data.coyoteTime; // Reset wall coyote time when touching a wall
            }

            // 2 checks needed for both sides of the player
            LastOnWallTime = Mathf.Max(LastOnWallRightTime, LastOnWallLeftTime);
        }

        if (isJumping && rb.linearVelocity.y < 0)
        {
            isJumping = false;

            if(!isWallJumping)
                _isJumpFalling = true;
        }

        if (isWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
        {
            isWallJumping = false;
        }

        if (LastOnGroundTime > 0 && !isJumping && !isWallJumping)
        {
            _isJumpCut = false;

            if(!isJumping)
                _isJumpFalling = false;
        }

        if (!isDashing)
        {
            if (CanJump() && LastPressedJumpTime > 0)
            {
                isJumping = true;
                isWallJumping = false;
                _isJumpCut = false;
                _isJumpFalling = false;
                Jump();
            }

            else if (CanWallJump() && LastPressedJumpTime > 0)
            {
                isWallJumping = true;
                isJumping = false;
                _isJumpCut = false;
                _isJumpFalling = false;
                _wallJumpStartTime = Time.time;
                _lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

                WallJump(_lastWallJumpDir);
            }
        }

        if (CanDash() && LastPressedDashTime > 0)
        {
            // Đóng băng game trong một khoảng thời gian ngắn. Tạo hiệu ứng "bullet time"
            Sleep(Data.dashSleepTime);

            // Nếu không có đầu vào di chuyển, dash theo hướng mặt
            if (_moveInput == Vector2.zero)
                _lastDashDir = isFacingRight ? Vector2.right : Vector2.left;
            else
                _lastDashDir = _moveInput;

            isDashing = true;
            isJumping = false;
            isWallJumping = false;
            _isJumpCut = false;
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
            //Higher gravity if we've released the jump input or are falling
            if (isSliding)
            {
                SetGravityScale(0);
            }
            else if (rb.linearVelocity.y < 0 && _moveInput.y < 0)
            {
                //Much higher gravity if holding down
                SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
                //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -Data.maxFastFallSpeed));
            }
            else if (_isJumpCut)
            {
                //Higher gravity if jump button released
                SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -Data.maxFallSpeed));
            }
            else if ((isJumping || isWallJumping || _isJumpFalling) && Mathf.Abs(rb.linearVelocity.y) < Data.jumpHangTimeThreshold)
            {
                SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
            }
            else if (rb.linearVelocity.y < 0)
            {
                //Higher gravity if falling
                SetGravityScale(Data.gravityScale * Data.fallGravityMult);
                //Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -Data.maxFallSpeed));
            }
            else
            {
                //Default gravity if standing on a platform or moving upwards
                SetGravityScale(Data.gravityScale);
            }
        }
        else
        {
            //No gravity when dashing (returns to normal once initial dashAttack phase over)
            SetGravityScale(0);
        }

        bool isCurrentlyGrounded = Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, _groundLayer);

        if (isCurrentlyGrounded && !_wasGrounded)
        {
            // just landed this frame
            OnLand?.Invoke(this, EventArgs.Empty);
        }

        _wasGrounded = isCurrentlyGrounded;

        bool isMoving = Mathf.Abs(_moveInput.x) > 0.1f;

        if (isCurrentlyGrounded && isMoving && !_wasMoving)
        {
            OnWalk?.Invoke(this, EventArgs.Empty);
        }

        if (isCurrentlyGrounded && !isMoving && _wasMoving)
        {
            OnStopWalk?.Invoke(this, EventArgs.Empty);
        }

        _wasMoving = isMoving;

    }

    private void FixedUpdate()
    {
        if (!isDashing)
        {
            if (isWallJumping)
                Run(Data.wallJumpRunLerp);
            else
                Run(1f);
        }

        else if (_isDashAttacking)
        {
            //Dashing movement handled in Dash coroutine
            Run(Data.dashEndRunLerp);
        }

        if (isSliding)
            Slide();
    }

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

    private void Sleep(float duration)
    {
        StartCoroutine(nameof(PerformSleep), duration   );
    }

    private IEnumerator PerformSleep(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }

    private void Run(float lerpAmount)
    {
        // Tính toán vận tốc mục tiêu dựa trên đầu vào di chuyển và tốc độ tối đa
        float targetSpeed = _moveInput.x * Data.runMaxSpeed;

        // Giảm tốc độ mục tiêu nếu đang nhảy và vận tốc dọc âm
        targetSpeed = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, lerpAmount);
    
        float accelRate;

        // Chọn tỷ lệ gia tốc dựa trên việc có chạm đất hay không
        if (LastOnGroundTime > 0)
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
        else
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.runAccelInAirMultiplier : Data.runDeccelAmount * Data.runDeccelInAirMultiplier;

        if ((isJumping || isWallJumping || _isJumpFalling) && Mathf.Abs(rb.linearVelocity.y) < Data.jumpHangTimeThreshold)
        {
            accelRate *= Data.jumpHangAccelMultiplier;
            targetSpeed *= Data.jumpHangMaxSpeedMultiplier;
        }

        // Tính toán vận tốc mới bằng cách di chuyển dần về phía vận tốc mục tiêu
        if (Data.doConserveMomentum && Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f)
        {
            // Giữ nguyên vận tốc hiện tại nếu đang di chuyển nhanh hơn vận tốc mục tiêu và cùng hướng
            accelRate = 0;
        }

        // Tính toán sự khác biệt về tốc độ và áp dụng lực để đạt được vận tốc mục tiêu
        float speedDif = targetSpeed - rb.linearVelocity.x;

        // Áp dụng lực dựa trên sự khác biệt về tốc độ và tỷ lệ gia tốc
        float movement = speedDif * accelRate;

        // Áp dụng lực vào Rigidbody2D
        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        /*
            Cách tính vận tốc khi sử dụng AddForce()
            rb.velocity = new Vector2(rb.velocity.x + (Time.fixedDeltaTime * speedDif * accelRate) / rb.mass, rb.velocity.y);
            Giải thích: 
            Time.fixedDeltaTime là thời gian giữa các khung FixedUpdate, rb.mass là khối lượng của Rigidbody2D
         */
    }

    private void Turn()
    {
        // Đảo ngược hướng mặt của nhân vật bằng cách lật trục x của localScale
        // Gọi hàm này khi muốn thay đổi hướng mặt của nhân vật
        Vector3 scale = transform.localScale;
        scale.x *= -1;

        // Áp dụng thay đổi localScale để lật hướng mặt
        transform.localScale = scale;

        isFacingRight = !isFacingRight;
    }

    private void Jump()
    {
        // Đảm bảo không thể gọi Jump nhiều lần liên tiếp
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;

        // Tính toán lực nhảy dựa trên lực nhảy cơ bản và vận tốc dọc hiện tại
        float force = Data.jumpForce;
        if (rb.linearVelocity.y < 0)
            force -= rb.linearVelocity.y;

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        OnJump?.Invoke(this, EventArgs.Empty);
    }

    private void WallJump(int dir)
    {
        // Đảm bảo không thể gọi WallJump nhiều lần liên tiếp
        LastPressedJumpTime = 0;
        LastOnGroundTime = 0;
        LastOnWallRightTime = 0;
        LastOnWallLeftTime = 0;

        Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
        force.x *= -dir; // Đảo ngược lực theo hướng tường

        if (Mathf.Sign(rb.linearVelocity.x) != Mathf.Sign(force.x))
            force.x -= rb.linearVelocity.x;

        if (rb.linearVelocity.y < 0)
            force.y -= rb.linearVelocity.y;

        rb.AddForce(force, ForceMode2D.Impulse);
    }

    private IEnumerator StartDash(Vector2 dir)
    {
        // Về tổng quan, dash method này dựa trên Celeste
        // dash sẽ có 2 giai đoạn: dashAttack và dashEnd

        LastOnGroundTime = 0;
        LastPressedDashTime = 0;

        float startTime = Time.time;

        _dashesLeft--;
        _isDashAttacking = true;

        SetGravityScale(0);

        while (Time.time - startTime <= Data.dashAttackTime)
        {
            rb.linearVelocity = dir.normalized * Data.dashSpeed;
            // Dừng vòng lặp cho đến khung hình tiếp theo
            // Đây là cách để tạo hiệu ứng dash liên tục trong một khoảng thời gian
            yield return null;
        }

        startTime = Time.time;

        _isDashAttacking = false;

        // Bắt đầu giai đoạn dashEnd, nơi người chơi có thể kiểm soát hướng di chuyển nhưng vẫn giới hạn gia tốc
        SetGravityScale(Data.gravityScale);
        rb.linearVelocity = dir.normalized * Data.dashEndSpeed;

        while (Time.time - startTime <= Data.dashEndTime)
        {
            yield return null;
        }

        // Kết thúc dash
        isDashing = false;
    }

    // Tạm dừng game trong một khoảng thời gian ngắn
    private IEnumerator RefillDash(int amount)
    {
        _dashRefilling = true;
        yield return new WaitForSeconds(Data.dashRefillTime);
        _dashRefilling = false;
        _dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
    }

    private void Slide()
    {
        // Hoạt động như Run nhưng theo phương thẳng đứng
        float speedDif = Data.slideSpeed - rb.linearVelocity.y;
        float movement = speedDif * Data.slideAccel;

        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

        rb.AddForce(movement * Vector2.up);
    }

    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != isFacingRight)
            Turn();
    }

    private bool CanJump()
    {
        return LastOnGroundTime > 0 && !isJumping;
    }

    private bool CanWallJump()
    {
        return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && (!isWallJumping ||
                     (LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
    }

    private bool CanJumpCut()
    {
        return isJumping && rb.linearVelocity.y > 0;
    }

    private bool CanWallJumpCut()
    {
        return isWallJumping && rb.linearVelocity.y > 0;
    }

    private bool CanDash()
    {
        if (!isDashing && _dashesLeft < Data.dashAmount && LastOnGroundTime > 0 && !_dashRefilling)
        {
            StartCoroutine(nameof(RefillDash), 1);
        }

        return _dashesLeft > 0;
    }

    public bool CanSlide()
    {
        if (LastOnWallTime > 0 && !isJumping && !isWallJumping && LastOnGroundTime <= 0)
            return true;
        else
            return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundCheck.position, _groundCheckSize);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(_fromWallCheckPoint.position, _wallCheckSize);
        Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
    }

    private void PlayerInteract_OnDead(object sender, EventArgs e)
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        this.enabled = false;
    }

    private void OnDestroy()
    {
        PlayerInteract.Instance.OnDead -= PlayerInteract_OnDead;
    }
}