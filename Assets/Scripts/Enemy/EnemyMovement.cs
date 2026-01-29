using System;
using System.Collections;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;
    public event EventHandler<bool> OnFaceDirectionChanged;

    public EnemyData Data;
    public Rigidbody2D rb { get; private set; }
    public bool isFacingRight { get; private set; } = true;
    public bool isKnockedBack { get; private set; }

    private Vector2 _currentVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 direction, float currentMaxSpeed, float accelAmount, float deccelAmount)
    {
        if (isKnockedBack) return;

        // 1. Kiểm tra hướng mặt
        if (direction.x != 0)
        {
            CheckDirectionToFace(direction.x > 0);
        }

        // 2. Tính toán vật lý (Logic giống hệt PlayerMovement.Run)
        float targetSpeed = direction.x * currentMaxSpeed;
        
        // Lerp nhẹ vận tốc (tùy chọn, giống Player)
        targetSpeed = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, 1f);

        float accelRate;

        // Chọn gia tốc (Accel) hay giảm tốc (Deccel)
        if (Mathf.Abs(targetSpeed) > 0.01f)
            accelRate = accelAmount;
        else
            accelRate = deccelAmount;

        // Bảo toàn động lượng (Momentum)
        if (Data.doConserveMomentum && Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetSpeed) && 
            Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f)
        {
            accelRate = 0;
        }

        // Áp dụng lực
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);

        // Kích hoạt Event cho Animation
        if (Mathf.Abs(direction.x) > 0.1f) 
            OnStartMoving?.Invoke(this, EventArgs.Empty);
        else 
            OnStopMoving?.Invoke(this, EventArgs.Empty);
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

        OnFaceDirectionChanged?.Invoke(this, isFacingRight);
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        StartCoroutine(KnockbackRoutine(direction, force));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force)
    {
        isKnockedBack = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.2f); // Hardcode hoặc đưa vào Data
        isKnockedBack = false;
    }
}