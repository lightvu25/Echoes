using UnityEngine;

[CreateAssetMenu(menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Wander")]
    public float wanderMaxSpeed;
    public float wanderAcceleration;
    [HideInInspector] public float wanderAccelAmount;
    public float wanderDecceleration;
    [HideInInspector] public float wanderDeccelAmount;
    public float wanderRadius = 3f;
    public float wanderWaitTimeMin = 1f;
    public float wanderWaitTimeMax = 3f;

    [Space(20)]

    [Header("Chase")]
    public float runMaxSpeed;
    public float runAcceleration;
    [HideInInspector] public float runAccelAmount;
    public float runDecceleration;
    [HideInInspector] public float runDeccelAmount;

    [Space(5)]
    [Range(0f, 1)] public float accelInAir;
    [Range(0f, 1)] public float deccelInAir;
    [Space(5)]
    public bool doConserveMomentum = true;

    [Space(20)]

    [Header("Attack")]
    public int attackBase;
    public int attackRange;
    public int attackSpeed;
    public float attackCooldown;

    [Space(20)]

    [Header("Senses")]
    public float visionRange;
    public float fovAngle;
    public float perceptionRange;
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;

    private void OnValidate()
    {
        wanderAccelAmount = (50 * wanderAcceleration) / runMaxSpeed;
        wanderDeccelAmount = (50 * wanderDecceleration) / runMaxSpeed;

        wanderAcceleration = Mathf.Clamp(wanderAcceleration, 0.01f, runMaxSpeed);
        wanderDecceleration = Mathf.Clamp(wanderDecceleration, 0.01f, runMaxSpeed);

        runAccelAmount = (50 * runAcceleration) / runMaxSpeed;
        runDeccelAmount = (50 * runDecceleration) / runMaxSpeed;

        runAcceleration = Mathf.Clamp(runAcceleration, 0.01f, runMaxSpeed);
        runDecceleration = Mathf.Clamp(runDecceleration, 0.01f, runMaxSpeed);
    }
}