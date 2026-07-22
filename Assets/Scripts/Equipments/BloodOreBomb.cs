using System.Collections;
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
        owner = attacker;
    }

    private void OnEnable()
    {
        StartCoroutine(ExplosionRoutine());
    }

    private IEnumerator ExplosionRoutine()
    {
        yield return new WaitForSeconds(explosionDelay);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, enemyLayer);
        foreach (var hit in hits)
        {
            var health = hit.GetComponent<HealthSystem>();
            if (health != null && !health.IsDead)
            {
                DamageInfo dInfo = DamageInfo.Create(baseDamage, owner != null ? owner : gameObject);
                health.TakeDamage(dInfo);
            }
        }

        // Potential VFX trigger here

        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
