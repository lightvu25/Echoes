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
        if (projectilePrefab == null || firePoint == null) return;

        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        GameObject proj = ObjectPoolManager.SpawnObject(projectilePrefab, firePoint.position, Quaternion.identity, ObjectPoolManager.PoolType.Projectile);

        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
            projectile.SetOwner(gameObject);

        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb != null)
            projRb.linearVelocity = direction * projectileSpeed;
    }
}
