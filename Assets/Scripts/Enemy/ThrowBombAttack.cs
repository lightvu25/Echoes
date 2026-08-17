using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// An enemy attack behavior that spawns and throws a bomb projectile in a parabolic arc.
/// Implements IEnemyAttack so it can be triggered by the EnemyBrain.
/// </summary>
public class ThrowBombAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;

    public bool IsAttacking { get; private set; }

    [Header("Bomb Attack Settings")]
    [Tooltip("The BombProjectile prefab to spawn.")]
    [SerializeField] private GameObject bombPrefab;
    
    [Tooltip("The position where the bomb is spawned before being thrown.")]
    [SerializeField] private Transform throwPoint;
    
    [Tooltip("The horizontal force applied to the bomb. Flips based on facing direction.")]
    [SerializeField] private float throwForceX = 5f;
    
    [Tooltip("The vertical force applied to the bomb for the arc.")]
    [SerializeField] private float throwForceY = 8f;
    
    [Tooltip("The duration the enemy is locked in the attack state.")]
    [SerializeField] private float attackDuration = 1.5f;
    
    [Tooltip("Delay before the bomb is actually spawned (sync with animation windup).")]
    [SerializeField] private float throwDelay = 0.5f;
    
    [Tooltip("Amount of torque applied to make the bomb spin in the air.")]
    [SerializeField] private float torqueAmount = -30f;

    [Header("Aim Settings")]
    [Tooltip("If true, perfectly calculates the trajectory to hit the player.")]
    [SerializeField] private bool predictiveAiming = true;

    [Tooltip("How high above the target the bomb should arc. Higher = loopier throw.")]
    [SerializeField] private float arcHeight = 3f;

    private Coroutine attackCoroutine;
    private EnemySensor sensor;

    private void Awake()
    {
        sensor = GetComponentInParent<EnemySensor>();
    }

    /// <summary>
    /// Called by the EnemyBrain to start the attack sequence.
    /// </summary>
    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        
        IsAttacking = true;
        OnAttackStarted?.Invoke(this, EventArgs.Empty);
        
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
            
        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    /// <summary>
    /// Called by the EnemyBrain to forcibly interrupt the attack.
    /// </summary>
    public void CancelAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        IsAttacking = false;
    }

    private IEnumerator AttackRoutine()
    {
        // Get attack speed from the brain data
        float speed = 1f;
        EnemyBrain brain = GetComponentInParent<EnemyBrain>();
        if (brain != null && brain.Data != null && brain.Data.attackSpeed > 0)
        {
            speed = brain.Data.attackSpeed;
        }

        // Wait for the animation windup!
        yield return new WaitForSeconds(throwDelay / speed);

        ThrowBomb();

        // Wait for the REST of the animation/attack sequence to finish
        yield return new WaitForSeconds((attackDuration - throwDelay) / speed);

        IsAttacking = false;
        OnAttackFinished?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowBomb()
    {
        if (bombPrefab == null || throwPoint == null) 
        {
            Debug.LogWarning($"[{gameObject.name}] [ThrowBombAttack] Missing bombPrefab or throwPoint reference.");
            return;
        }

        // Determine facing direction (assuming lossyScale.x determines flip)
        float facingDir = Mathf.Sign(transform.lossyScale.x);

        // Spawn the bomb from the object pool
        GameObject bombObj = ObjectPoolManager.SpawnObject(
            bombPrefab, 
            throwPoint.position, 
            Quaternion.identity, 
            ObjectPoolManager.PoolType.Projectile
        );

        if (bombObj != null)
        {
            // Assign owner so it doesn't blow up the enemy itself
            BombProjectile bomb = bombObj.GetComponent<BombProjectile>();
            if (bomb != null)
            {
                EnemyBrain brain = GetComponentInParent<EnemyBrain>();
                EnemyData data = brain != null ? brain.Data : null;
                if (data != null)
                {
                    float damageMultiplier = BurdenManager.Instance != null
                        ? BurdenManager.Instance.CurrentDamageMultiplier
                        : 1f;
                    int scaledDamage = Mathf.RoundToInt(data.attackBase * damageMultiplier);
                    bomb.SetOwner(gameObject, scaledDamage, data.knockbackForce * 2f);
                }
                else
                {
                    bomb.SetOwner(gameObject);
                }
                
                // Ignore physical collision between the bomb and the thrower
                Collider2D bombCol = bombObj.GetComponent<Collider2D>();
                Collider2D[] throwerCols = transform.root.GetComponentsInChildren<Collider2D>();
                if (bombCol != null)
                {
                    foreach (var tCol in throwerCols)
                    {
                        if (tCol != bombCol) // Just in case
                        {
                            Physics2D.IgnoreCollision(bombCol, tCol);
                        }
                    }
                }
            }

            // Apply forces
            Rigidbody2D rb = bombObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                
                Vector2 force = new Vector2(throwForceX * facingDir, throwForceY) * rb.mass;

                if (predictiveAiming && sensor != null && sensor.TargetPlayer != null)
                {
                    Vector2 targetPos = sensor.TargetPlayer.position;
                    Vector2 startPos = throwPoint.position;
                    
                    float gravity = Physics2D.gravity.y * rb.gravityScale;
                    
                    // We only calculate if gravity is pulling downwards
                    if (gravity < 0)
                    {
                        // Scale the arc height based on horizontal distance so close targets don't shoot straight up!
                        float dist = Mathf.Abs(targetPos.x - startPos.x);
                        float dynamicArcHeight = Mathf.Clamp(dist * 0.4f, 0.5f, arcHeight);
                        
                        // Calculate required peak height above the highest point
                        float height = Mathf.Max(targetPos.y - startPos.y, 0f) + dynamicArcHeight;
                        
                        // vy = sqrt(-2 * g * h)
                        float vy = Mathf.Sqrt(-2f * gravity * height);
                        
                        // Time to reach peak
                        float timeUp = vy / -gravity;
                        
                        // Time to fall from peak to target
                        float fallDistance = height - (targetPos.y - startPos.y);
                        float timeDown = Mathf.Sqrt(2f * fallDistance / -gravity);
                        
                        float totalTime = timeUp + timeDown;
                        
                        // vx = dx / t
                        float vx = (targetPos.x - startPos.x) / totalTime;
                        
                        // Force = Mass * Velocity for Impulse
                        force = new Vector2(vx, vy) * rb.mass;
                        
                        BombProjectile bombProj = bombObj.GetComponent<BombProjectile>();
                        if (bombProj != null)
                        {
                            bombProj.FlyToTarget(totalTime);
                        }
                    }
                }
                else
                {
                    // Fallback to inspector force if aimbot is disabled
                    force = new Vector2(throwForceX * facingDir, throwForceY);
                }

                rb.AddForce(force, ForceMode2D.Impulse);
                
                // Impulse torque for the spin (multiplied by facingDir so it rolls correctly)
                rb.AddTorque(torqueAmount * facingDir, ForceMode2D.Impulse);
            }
        }
    }
}
