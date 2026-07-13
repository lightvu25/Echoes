using System;
using UnityEngine;

public class FlyingMovement : MonoBehaviour, IEnemyMovement
{
    public event EventHandler OnIdle;
    public event EventHandler OnMoving;

    [SerializeField] private EnemyData data;
    [SerializeField] private float hoverHeight = 3f;
    [SerializeField] private float hoverOscillation = 0.5f;
    [SerializeField] private float oscillationSpeed = 2f;

    public Rigidbody2D Rb { get; private set; }
    public bool IsFacingRight { get; private set; }
    public bool IsGroundedAhead => true;
    public bool IsWallAhead => false;
    public bool IsKnockedBack { get; private set; }

    private float oscillationPhase;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        IsFacingRight = transform.localScale.x > 0;
        Rb.gravityScale = 0f;
        oscillationPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    }

    public void Move(Vector2 direction, float maxSpeed, float accelAmount, float deccelAmount)
    {
        if (IsKnockedBack) return;
        if (direction.x != 0) FaceDirection(direction.x > 0);

        float oscillation = Mathf.Sin(Time.time * oscillationSpeed + oscillationPhase) * hoverOscillation;
        
        Vector2 targetVelocity = new Vector2(direction.x * maxSpeed, (direction.y * maxSpeed) + oscillation);

        Rb.linearVelocity = Vector2.Lerp(Rb.linearVelocity, targetVelocity, accelAmount * Time.deltaTime);

        if (direction.sqrMagnitude > 0.01f) OnMoving?.Invoke(this, EventArgs.Empty);
        else OnIdle?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        float oscillation = Mathf.Sin(Time.time * oscillationSpeed + oscillationPhase) * hoverOscillation;
        Rb.linearVelocity = new Vector2(0, oscillation);
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
}
