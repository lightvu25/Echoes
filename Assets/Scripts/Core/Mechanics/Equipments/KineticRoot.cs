using UnityEngine;
using System.Collections.Generic;

public class KineticRoot : MonoBehaviour
{
    [SerializeField] private float radius = 5f;
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private LayerMask enemyLayer;

    public void Initialize(GameObject attacker)
    {
        StopAllCoroutines();
        ExecuteShockwave();
        StartCoroutine(LifetimeRoutine());
    }

    private void OnEnable()
    {
    }

    private void OnDisable() { StopAllCoroutines(); }

    private System.Collections.IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        ObjectPoolManager.ReturnObjectToPool(gameObject); // Quick cleanup
    }

    private void ExecuteShockwave()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
        HashSet<Rigidbody2D> pushed = new HashSet<Rigidbody2D>();
        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            Rigidbody2D body = hit.GetComponentInParent<Rigidbody2D>();
            if (target != null && !target.IsDead && body != null &&
                body.bodyType == RigidbodyType2D.Dynamic && pushed.Add(body))
            {
                Vector2 direction = target.Transform.position - transform.position;
                if (direction.sqrMagnitude < 0.0001f) direction = Vector2.up;
                direction.Normalize();

                EnemyCombat enemyCombat = target as EnemyCombat;
                if (enemyCombat != null) enemyCombat.ApplyExternalKnockback(direction, knockbackForce);
                else body.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
}
