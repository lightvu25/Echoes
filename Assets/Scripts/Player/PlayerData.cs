using UnityEngine;

// Tạo một ScriptableObject để lưu trữ dữ liệu của người chơi
[CreateAssetMenu(menuName = "Player Data")]
public class PlayerData : MonoBehaviour
{
    [Header("Gravity")]
    [HideInInspector] public float gravityStrength;
    [HideInInspector] public float gravityScale;

    [Space(5)]
    public float fallGravityMult;
    public float maxFallSpeed;
    [Space(5)]
    public float fastFallGravityMult;

    public float maxFastFallSpeed;

    [Space(20)]

    [Header("Run")]
    public float runMaxSpeed; //Target speed we want the player to reach.
    public float runAcceleration; //The speed at which our player accelerates to max speed, can be set to runMaxSpeed for instant acceleration down to 0 for none at all
    [HideInInspector] public float runAccelAmount; //The actual force (multiplied with speedDiff) applied to the player.
    public float runDecceleration; //The speed at which our player decelerates from their current speed, can be set to runMaxSpeed for instant deceleration down to 0 for none at all
    [HideInInspector] public float runDeccelAmount; //Actual force (multiplied with speedDiff) applied to the player .
    [Space(5)]
    [Range(0f, 1)] public float accelInAir; //Multipliers applied to acceleration rate when airborne.
    [Range(0f, 1)] public float deccelInAir;
    [Space(5)]
    public bool doConserveMomentum = true;

    [Space(20)]

    [Header("Jump")]
    public float jumpHeight; //Height of the player's jump
    public float jumpTimeToApex; //Time between applying the jump force and reaching the desired jump height. These values also control the player's gravity and jump force.
    [HideInInspector] public float jumpForce; //The actual force applied (upwards) to the player when they jump.

    [Header("Both Jumps")]
    public float jumpCutGravityMult; //Multiplier to increase gravity if the player releases thje jump button while still jumping
    [Range(0f, 1)] public float jumpHangGravityMult; //Reduces gravity while close to the apex (desired max height) of the jump
    public float jumpHangTimeThreshold; //Speeds (close to 0) where the player will experience extra "jump hang". The player's velocity.y is closest to 0 at the jump's apex (think of the gradient of a parabola or quadratic function)
    [Space(0.5f)]
    public float jumpHangAccelerationMult;
    public float jumpHangMaxSpeedMult;

    [Header("Wall Jump")]
    public Vector2 wallJumpForce; //The actual force (this time set by us) applied to the player when wall jumping.
    [Space(5)]
    [Range(0f, 1f)] public float wallJumpRunLerp; //Reduces the effect of player's movement while wall jumping.
    [Range(0f, 1.5f)] public float wallJumpTime; //Time after wall jumping the player's movement is slowed for.
    public bool doTurnOnWallJump; //Player will rotate to face wall jumping direction

    [Space(20)]

    [Range(0f, 2f)] public float wallJumpDuration = 0.4f; // Duration of wall jump state

    // Multipliers for air acceleration and deceleration
    [Header("Air Control Multipliers")]
    [Range(0f, 1f)] public float runAccelInAirMultiplier = 0.5f;
    [Range(0f, 1f)] public float runDeccelInAirMultiplier = 0.5f;

    // Jump hang multipliers
    [Header("Jump Hang Multipliers")]
    [Range(0f, 2f)] public float jumpHangAccelMultiplier = 1.2f;
    [Range(0f, 2f)] public float jumpHangMaxSpeedMultiplier = 1.1f;

    [Header("Slide")]
    public float slideSpeed;
    public float slideAccel;

    [Header("Assists")]
    [Range(0.01f, 0.5f)] public float coyoteTime; //Grace period after falling off a platform, where you can still jump
    [Range(0.01f, 0.5f)] public float jumpInputBufferTime; //Grace period after pressing jump where a jump will be automatically performed once the requirements (eg. being grounded) are met.

    [Header("Dash")]
    public int dashAmount;
    public float dashSpeed;
    public float dashSleepTime; //Duration for which the game freezes when we press dash but before we read directional input and apply a force
    [Space(5)]
    public float dashAttackTime;
    [Space(5)]
    public float dashEndTime; //Time after you finish the inital drag phase, smoothing the transition back to idle (or any standard state)
    public Vector2 dashEndSpeed; //Slows down player, makes dash feel more responsive (used in Celeste)
    [Range(0f, 1f)] public float dashEndRunLerp; //Slows the affect of player movement while dashing
    [Space(5)]
    public float dashRefillTime;
    [Space(5)]
    [Range(0.01f, 0.5f)] public float dashInputBufferTime;

    //Unity Callback, called when the inspector updates
    private void OnValidate()
    {
        //Calculate gravity strength using the formula (gravity = 2 * jumpHeight / timeToJumpApex^2) 
        gravityStrength = -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);

        //Calculate the rigidbody's gravity scale (ie: gravity strength relative to unity's gravity value, see project settings/Physics2D)
        gravityScale = gravityStrength / Physics2D.gravity.y;

        //Calculate are run acceleration & deceleration forces using formula: amount = ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed
        runAccelAmount = (50 * runAcceleration) / runMaxSpeed;
        runDeccelAmount = (50 * runDecceleration) / runMaxSpeed;

        //Calculate jumpForce using the formula (initialJumpVelocity = gravity * timeToJumpApex)
        jumpForce = Mathf.Abs(gravityStrength) * jumpTimeToApex;

        runAcceleration = Mathf.Clamp(runAcceleration, 0.01f, runMaxSpeed);
        runDecceleration = Mathf.Clamp(runDecceleration, 0.01f, runMaxSpeed);
    }
}
