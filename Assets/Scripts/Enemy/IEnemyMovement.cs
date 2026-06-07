using UnityEngine;

public interface IEnemyMovement
{
    Rigidbody2D Rb { get; }
    bool IsFacingRight { get; }
    bool IsGroundedAhead { get; }
    bool IsWallAhead { get; }
    bool IsKnockedBack { get; }

    void Move(Vector2 direction, float maxSpeed, float accelAmount, float deccelAmount);
    void Stop();
    void FaceDirection(bool faceRight);
    void SetKnockedBack(bool value);
}
