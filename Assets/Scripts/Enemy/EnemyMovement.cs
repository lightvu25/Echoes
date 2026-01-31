using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public event EventHandler OnIdle;
    public event EventHandler OnPatrol;

    public EnemyData Data;
    public Rigidbody2D rb { get; private set; }
    public bool isFacingRight { get; private set; }

    public bool isGroundedAhead { get; private set; }
    public bool isWallAhead { get; private set; }
    public bool isKnockedBack { get; private set; }

    [Header("Detection Checks")]
    // Check Đất
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.5f, 0.5f);

    // Check Tường (CHỈ CẦN 1 CÁI)
    [Space(10)]
    [SerializeField] private Transform _wallCheck;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.2f, 1f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        isFacingRight = transform.localScale.x > 0;
    }

    private void Update()
    {
        PerformEnvironmentalChecks();
    }

    private void PerformEnvironmentalChecks()
    {
        if (Data == null) return;

        // 1. Check Đất
        isGroundedAhead = Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, Data.groundLayer);

        // 2. Check Tường
        // Vì Transform _wallCheck là con của Enemy, khi Enemy lật (Scale X -1), 
        // _wallCheck cũng tự động lật sang phía bên kia. Ta luôn check đúng hướng mặt.
        isWallAhead = Physics2D.OverlapBox(_wallCheck.position, _wallCheckSize, 0f, Data.wallLayer);
    }

    public void Move(Vector2 direction, float currentMaxSpeed, float accelAmount, float deccelAmount)
    {
        if (isKnockedBack) return;

        if (direction.x != 0)
        {
            CheckDirectionToFace(direction.x > 0);
        }

        // Logic di chuyển
        float targetSpeed = direction.x * currentMaxSpeed;
        targetSpeed = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, 10f * Time.deltaTime); // Dùng deltaTime ở Update hoặc fixedDeltaTime nếu logic khác

        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accelAmount : deccelAmount;

        if (Data.doConserveMomentum && Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f)
        {
            accelRate = 0;
        }

        float speedDif = targetSpeed - rb.linearVelocity.x;
        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        if (Mathf.Abs(direction.x) > 0.1f)
            OnPatrol?.Invoke(this, EventArgs.Empty);
        else
            OnIdle?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        OnIdle?.Invoke(this, EventArgs.Empty);
    }

    public void CheckDirectionToFace(bool isMovingRight)
    {
        if (isMovingRight != isFacingRight)
            Turn();
    }

    private void Turn()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (_groundCheck != null)
            Gizmos.DrawWireCube(_groundCheck.position, _groundCheckSize);

        Gizmos.color = Color.blue;
        if (_wallCheck != null)
            Gizmos.DrawWireCube(_wallCheck.position, _wallCheckSize);
    }
}