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
        bool isFacingRight = transform.localScale.x >= 0;
        Vector2 direction = isFacingRight ? Vector2.right : Vector2.left;
        
        Quaternion spawnRotation = isFacingRight ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);

        GameObject proj = ObjectPoolManager.SpawnObject(projectilePrefab, firePoint.position, spawnRotation, ObjectPoolManager.PoolType.Projectile);

        Projectile projectile = proj.GetComponent<Projectile>();
        projectile.SetOwner(gameObject);

        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        projRb.linearVelocity = direction * projectileSpeed;
    }
}
