using System;
using System.Collections;
using UnityEngine;

public class BombAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;

    [SerializeField] private EnemyData data;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDelay = 0.5f;
    [SerializeField] private float selfDamageMultiplier = 0.5f;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private GameObject explosionVFXPrefab;

    public bool IsAttacking { get; private set; }

    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        IsAttacking = true;
        OnAttackStarted?.Invoke(this, EventArgs.Empty);
        StartCoroutine(BombRoutine());
    }

    public void CancelAttack()
    {
        IsAttacking = false;
        StopAllCoroutines();
    }

    private IEnumerator BombRoutine()
    {
        yield return new WaitForSeconds(explosionDelay);

        Explode();

        IsAttacking = false;
        OnAttackFinished?.Invoke(this, EventArgs.Empty);
    }

    private void Explode()
    {
        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageableLayers);

        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || target.IsDead) continue;

            float dirX = target.Transform.position.x - transform.position.x;
            Vector2 knockbackDir = new Vector2(dirX, 0.5f).normalized;
            bool isSelf = hit.transform.root.gameObject == transform.root.gameObject;

            DamageInfo damageInfo = new DamageInfo
            {
                baseDamage = data != null ? data.attackBase : 15,
                flatBonus = 0,
                linearModifierSum = 0f,
                multiplicativeStack = isSelf ? selfDamageMultiplier : 1f,
                procCoefficient = 1f,
                knockbackDirection = knockbackDir,
                knockbackForce = data != null ? data.knockbackForce * 2f : 8f,
                hitFreezeTime = 0f,
                attacker = gameObject,
                damageSource = DamageSourceType.BombAttack,
                isCritical = false
            };

            target.TakeDamage(damageInfo);
        }

        // Explicitly kill the bomber
        EnemyCombat combat = GetComponentInParent<EnemyCombat>();
        if (combat != null && !combat.IsDead)
        {
            HealthSystem hs = combat.GetComponent<HealthSystem>();
            if (hs != null) hs.SetInvincible(false);

            combat.TakeDamage(new DamageInfo { baseDamage = 9999, attacker = gameObject, damageSource = DamageSourceType.Suicide });
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
