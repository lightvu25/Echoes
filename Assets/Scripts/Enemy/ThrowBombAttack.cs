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
    
    [Tooltip("Amount of torque applied to make the bomb spin in the air.")]
    [SerializeField] private float torqueAmount = -30f;

    private Coroutine attackCoroutine;

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
        ThrowBomb();

        // Wait for the animation/attack sequence to finish
        yield return new WaitForSeconds(attackDuration);

        IsAttacking = false;
        OnAttackFinished?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowBomb()
    {
        if (bombPrefab == null || throwPoint == null) 
        {
            Debug.LogWarning("[ThrowBombAttack] Missing bombPrefab or throwPoint reference.");
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
                bomb.SetOwner(gameObject);
            }

            // Apply forces
            Rigidbody2D rb = bombObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                
                // Impulse force for the throw arc
                Vector2 force = new Vector2(throwForceX * facingDir, throwForceY);
                rb.AddForce(force, ForceMode2D.Impulse);
                
                // Impulse torque for the spin (multiplied by facingDir so it rolls correctly)
                rb.AddTorque(torqueAmount * facingDir, ForceMode2D.Impulse);
            }
        }
    }
}
