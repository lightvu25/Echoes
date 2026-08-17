using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodOreBomb : MonoBehaviour
{
    [SerializeField] private float explosionDelay = 2f;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int baseDamage = 30;
    [SerializeField] private LayerMask enemyLayer;

    private GameObject owner;

    public void Initialize(GameObject attacker)
    {
        StopAllCoroutines();
        owner = attacker;
        StartCoroutine(ExplosionRoutine());
    }

    private void OnEnable()
    {
        owner = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        owner = null;
    }

    private IEnumerator ExplosionRoutine()
    {
        yield return new WaitForSeconds(explosionDelay);

        GameFeelManager.Instance?.ProcessExplosion(transform.position);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayer);
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead && damaged.Add(target))
            {
                DamageInfo dInfo = DamageInfo.Create(baseDamage, owner != null ? owner : gameObject);
                dInfo.damageSource = DamageSourceType.BombAttack;
                target.TakeDamage(dInfo);
            }
        }

        // Potential VFX trigger here

        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
