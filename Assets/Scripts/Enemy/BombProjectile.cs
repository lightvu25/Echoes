using System.Collections;
using UnityEngine;

/// <summary>
/// A projectile that acts as a bomb. It flies through the air, starts a fuse,
/// and explodes either when the fuse runs out or when it touches a valid layer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BombProjectile : MonoBehaviour
{
    [Header("Bomb Settings")]
    [Tooltip("Time before the bomb explodes automatically.")]
    [SerializeField] private float fuseTime = 2f;
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private int explosionDamage = 15;
    [SerializeField] private float knockbackForce = 10f;
    
    [Header("Layers")]
    [Tooltip("Layers that can take damage from the explosion.")]
    [SerializeField] private LayerMask damageableLayers;
    [Tooltip("Layers that trigger an immediate explosion upon contact (e.g. walls/ground).")]
    [SerializeField] private LayerMask groundLayers;
    
    [Header("VFX")]
    [SerializeField] private GameObject explosionVFXPrefab;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool hasExploded = false;
    private Coroutine fuseCoroutine;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Sets the owner of this bomb to prevent it from damaging its thrower,
    /// and to correctly attribute the damage source.
    /// </summary>
    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }

    private void OnEnable()
    {
        hasExploded = false;
        col.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
        rb.linearVelocity = Vector2.zero;

        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
        }
        fuseCoroutine = StartCoroutine(FuseRoutine());
    }

    private void OnDisable()
    {
        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
            fuseCoroutine = null;
        }
    }

    private IEnumerator FuseRoutine()
    {
        yield return new WaitForSeconds(fuseTime);
        if (!hasExploded)
        {
            Explode();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasExploded) return;

        int layer = collision.gameObject.layer;
        
        bool hitDamageable = (damageableLayers.value & (1 << layer)) != 0;
        bool hitGround = (groundLayers.value & (1 << layer)) != 0;

        // Ignore direct hits on the thrower (bomb passes through or bounces off)
        if (hitDamageable && owner != null && collision.gameObject == owner)
        {
            return;
        }

        if (hitDamageable || hitGround)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (explosionVFXPrefab != null)
        {
            ObjectPoolManager.SpawnObject(explosionVFXPrefab, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageableLayers);
        foreach (Collider2D hit in hits)
        {
            // Do not damage the entity that threw the bomb
            if (owner != null && hit.gameObject == owner) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                // Calculate knockback strictly away from the bomb
                Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                
                // Add a slight upward bump to simulate being lifted by the explosion
                knockDir.y += 0.5f;
                knockDir.Normalize();

                DamageInfo info = DamageInfo.CreateWithKnockback(
                    explosionDamage, 
                    owner != null ? owner : gameObject, 
                    knockDir, 
                    knockbackForce
                );
                
                damageable.TakeDamage(info);
            }
        }

        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
            fuseCoroutine = null;
        }

        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
