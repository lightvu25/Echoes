using System;
using UnityEngine;

public class GroundMovement : MonoBehaviour, IEnemyMovement
{
    public event EventHandler OnIdle;
    public event EventHandler OnPatrol;

    [SerializeField] private EnemyData data;

    [Header("Detection Checks")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.5f, 0.5f);

    [Space(10)]
    [SerializeField] private Transform _wallCheck;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.2f, 1f);

    [Space(10)]
    [SerializeField] private Transform _ledgeCheck;
    [SerializeField] private Vector2 _ledgeCheckSize = new Vector2(0.5f, 0.5f);

    public Rigidbody2D Rb { get; private set; }
    public bool IsFacingRight { get; private set; }
    public bool IsGroundedAhead { get; private set; }
    public bool IsWallAhead { get; private set; }
    public bool IsKnockedBack { get; private set; }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        IsFacingRight = transform.localScale.x > 0;
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    private void FixedUpdate()
    {
        if (data == null) return;
        
        // Wall Check
        Vector2 dir = ((Vector2)_wallCheck.position - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, _wallCheck.position);
        IsWallAhead = Physics2D.BoxCast(transform.position, _wallCheckSize, 0f, dir, dist, data.wallLayer);

        // Ledge Check
        if (_ledgeCheck != null)
        {
            Collider2D hit = Physics2D.OverlapBox(_ledgeCheck.position, _ledgeCheckSize, 0f, data.groundLayer);
            IsGroundedAhead = hit != null; // Nếu hộp còn chạm đất -> True, hụt đất (vực) -> False
        }
        else
        {
            IsGroundedAhead = Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, data.groundLayer);
        }
    }

    public void Move(Vector2 direction, float maxSpeed, float accelAmount, float deccelAmount)
    {
        if (IsKnockedBack) return;
        if (direction.x != 0) FaceDirection(direction.x > 0);

        float targetSpeed = Mathf.Lerp(Rb.linearVelocity.x, direction.x * maxSpeed, 10f * Time.deltaTime);
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? accelAmount : deccelAmount;

        if (data.doConserveMomentum &&
            Mathf.Abs(Rb.linearVelocity.x) > Mathf.Abs(targetSpeed) &&
            Mathf.Sign(Rb.linearVelocity.x) == Mathf.Sign(targetSpeed) &&
            Mathf.Abs(targetSpeed) > 0.01f)
        {
            accelRate = 0;
        }

        Rb.AddForce((targetSpeed - Rb.linearVelocity.x) * accelRate * Vector2.right, ForceMode2D.Force);

        if (Mathf.Abs(direction.x) > 0.1f) OnPatrol?.Invoke(this, EventArgs.Empty);
        else OnIdle?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        Rb.linearVelocity = new Vector2(0, Rb.linearVelocity.y);
        OnIdle?.Invoke(this, EventArgs.Empty);
    }

    public void FaceDirection(bool faceRight)
    {
        if (faceRight != IsFacingRight) Turn();
    }

    public void SetKnockedBack(bool value) => IsKnockedBack = value;

    private void Turn()
    {
        IsFacingRight = !IsFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (_groundCheck != null) Gizmos.DrawWireCube(_groundCheck.position, _groundCheckSize);
        
        Gizmos.color = Color.blue;
        if (_wallCheck != null) Gizmos.DrawWireCube(_wallCheck.position, _wallCheckSize);

        Gizmos.color = Color.yellow;
        if (_ledgeCheck != null) Gizmos.DrawWireCube(_ledgeCheck.position, _ledgeCheckSize);   
    }
}
