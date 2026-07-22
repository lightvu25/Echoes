using UnityEngine;

public class KineticRoot : MonoBehaviour
{
    [SerializeField] private float radius = 5f;
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private LayerMask enemyLayer;

    private GameObject owner;

    public void Initialize(GameObject attacker)
    {
        owner = attacker;
    }

    private void OnEnable()
    {
        ExecuteShockwave();
        StartCoroutine(LifetimeRoutine());
    }

    private System.Collections.IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        ObjectPoolManager.ReturnObjectToPool(gameObject); // Quick cleanup
    }

    private void ExecuteShockwave()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
        foreach (var hit in hits)
        {
            var health = hit.GetComponent<HealthSystem>();
            if (health != null && !health.IsDead)
            {
                Vector2 direction = (hit.transform.position - transform.position).normalized;
                
                // Zero base damage, but applies knockback
                DamageInfo dInfo = DamageInfo.CreateWithKnockback(0, owner != null ? owner : gameObject, direction, knockbackForce);
                health.TakeDamage(dInfo);
            }
        }
    }
}
