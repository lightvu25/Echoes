using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float defaultGravityScale = 0f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private LayerMask hitLayers;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool hasHit;
    private Coroutine lifetimeCoroutine;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    // Resets all pooled state every time this object is reactivated.
    private void OnEnable()
    {
        hasHit = false;
        col.enabled = true;
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
    }

    private void OnDisable()
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
            lifetimeCoroutine = null;
        }
    }

    public void SetOwner(GameObject attacker) => owner = attacker;

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (hitLayers != (hitLayers | (1 << other.gameObject.layer))) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null && !target.IsDead)
        {
            Vector2 knockDir = (other.transform.position - transform.position).normalized;
            DamageInfo info = DamageInfo.CreateWithKnockback(damage, owner, knockDir, knockbackForce);
            info.damageSource = "Projectile";
            target.TakeDamage(info);
        }

        hasHit = true;
        col.enabled = false;
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
