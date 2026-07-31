using System;
using System.Collections;
using UnityEngine;

public class RangedAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;

    [SerializeField] private EnemyData data;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float attackDuration = 0.5f;
    [SerializeField] private bool aimAtPlayer = false;

    public bool IsAttacking { get; private set; }

    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        IsAttacking = true;
        OnAttackStarted?.Invoke(this, EventArgs.Empty);
        StartCoroutine(AttackRoutine());
    }

    public void CancelAttack()
    {
        IsAttacking = false;
        StopAllCoroutines();
    }

    private IEnumerator AttackRoutine()
    {
        FireProjectile();
        yield return new WaitForSeconds(attackDuration);
        IsAttacking = false;
        OnAttackFinished?.Invoke(this, EventArgs.Empty);
    }

    private void FireProjectile()
    {
        Vector2 direction;
        var sensor = GetComponentInParent<EnemySensor>();
        if (aimAtPlayer && sensor != null && sensor.TargetPlayer != null)
        {
            // Aim at the player's center (assuming TargetPlayer has a collider, or just use its position)
            direction = ((Vector2)sensor.TargetPlayer.position - (Vector2)firePoint.position).normalized;
        }
        else
        {
            bool isFacingRight = transform.localScale.x >= 0;
            direction = isFacingRight ? Vector2.right : Vector2.left;
        }
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion spawnRotation = Quaternion.Euler(0, 0, angle);

        GameObject proj = ObjectPoolManager.SpawnObject(projectilePrefab, firePoint.position, spawnRotation, ObjectPoolManager.PoolType.Projectile);

        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetOwner(gameObject);
        }

        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.linearVelocity = direction * projectileSpeed;
        }
    }
}
