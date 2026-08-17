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
    private bool isFlying = false;
    private Coroutine fuseCoroutine;
    private Coroutine flightCoroutine;
    private GameObject owner;
    private int runtimeExplosionDamage;
    private float runtimeKnockbackForce;

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
        SetOwner(newOwner, explosionDamage, knockbackForce);
    }

    public void SetOwner(GameObject newOwner, int configuredDamage, float configuredKnockback)
    {
        owner = newOwner;
        runtimeExplosionDamage = Mathf.Max(1, configuredDamage);
        runtimeKnockbackForce = Mathf.Max(0f, configuredKnockback);
        if (owner != null && col != null)
        {
            Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();
            foreach (var ownerCol in ownerColliders)
            {
                Physics2D.IgnoreCollision(col, ownerCol);
            }
        }
    }

    private void OnEnable()
    {
        hasExploded = false; // CRITICAL: Reset state for pooled bombs!
        isFlying = false;
        owner = null;
        runtimeExplosionDamage = explosionDamage;
        runtimeKnockbackForce = knockbackForce;
        col.enabled = true;
        col.isTrigger = false; // Make sure it can physically bounce/collide
        
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
        }
        fuseCoroutine = StartCoroutine(FuseRoutine());
        
        if (flightCoroutine != null)
        {
            StopCoroutine(flightCoroutine);
            flightCoroutine = null;
        }
    }

    private void OnDisable()
    {
        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
            fuseCoroutine = null;
        }
        if (flightCoroutine != null)
        {
            StopCoroutine(flightCoroutine);
            flightCoroutine = null;
        }
    }

    public void FlyToTarget(float flightTime)
    {
        if (flightCoroutine != null) StopCoroutine(flightCoroutine);
        flightCoroutine = StartCoroutine(FlightRoutine(flightTime));
    }

    private IEnumerator FlightRoutine(float flightTime)
    {
        isFlying = true;
        col.isTrigger = true; // Become a ghost
        
        yield return new WaitForSeconds(flightTime);
        
        isFlying = false;
        col.isTrigger = false; // Become solid again
        
        // Let gravity and HandleImpact take over from here. 
        // If it's in the air, it falls. If it's in the ground, OnCollisionEnter2D triggers immediately.
    }

    private IEnumerator FuseRoutine()
    {
        yield return new WaitForSeconds(fuseTime);
        if (!hasExploded)
        {
            Explode();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFlying) return;
        
        bool hitFloor = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                hitFloor = true;
                break;
            }
        }
        
        HandleImpact(collision.gameObject, hitFloor);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFlying) return;
        HandleImpact(collision.gameObject, false);
    }

    private void FixedUpdate()
    {
        // If we are ghosting through the air and falling downwards...
        if (isFlying && rb != null && rb.linearVelocity.y < 0)
        {
            // BoxCast downwards to see if we are about to hit the floor or a platform
            float checkDistance = (rb.linearVelocity.magnitude * Time.fixedDeltaTime) + 0.1f;
            
            // BoxCast slightly smaller than the collider to avoid snagging on tight corners
            Vector2 boxSize = col.bounds.size * 0.9f;
            RaycastHit2D hit = Physics2D.BoxCast(col.bounds.center, boxSize, 0f, rb.linearVelocity.normalized, checkDistance, groundLayers);
            
            // If we hit something on the ground layer, and its normal points upwards (it's a floor, not a vertical wall!)
            if (hit.collider != null)
            {
                // Stop ghosting immediately
                isFlying = false;
                col.isTrigger = false;
                
                if (flightCoroutine != null)
                {
                    StopCoroutine(flightCoroutine);
                    flightCoroutine = null;
                }
                
                // If we hit a floor, snap and stick to it.
                if (hit.normal.y > 0.5f)
                {
                    transform.position = hit.centroid;
                    HandleImpact(hit.collider.gameObject, true);
                }
                // If we hit a wall, we just become solid and let physics bounce it!
            }
        }
    }

    private void HandleImpact(GameObject hitObj, bool isFloorHit)
    {
        Debug.Log($"[BombProjectile] HandleImpact with {hitObj.name} (Layer {hitObj.layer})");
        if (hasExploded) return;

        int layer = hitObj.layer;
        
        bool hitDamageable = (damageableLayers.value & (1 << layer)) != 0;
        bool hitGround = (groundLayers.value & (1 << layer)) != 0;

        Debug.Log($"[BombProjectile] hitDamageable: {hitDamageable}, hitGround: {hitGround}");

        // Ignore direct hits on the thrower (bomb passes through or bounces off)
        if (hitDamageable && owner != null && hitObj.transform.root.gameObject == owner.transform.root.gameObject)
        {
            Debug.Log($"[BombProjectile] Ignoring impact on owner {owner.name}");
            return;
        }

        if (hitDamageable || hitGround)
        {
            // Do not explode on impact, wait for the fuse timer!
            
            // Stop the bomb dead in its tracks when it hits the ground so it doesn't roll or chase the player
            if (hitGround && rb != null && isFloorHit)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                // Optional: make it kinematic so nothing else pushes it
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
    }

    private void Explode()
    {
        Debug.Log($"[BombProjectile] Explode called! hasExploded={hasExploded} on {gameObject.name}");
        if (hasExploded) return;
        hasExploded = true;
        col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        GameFeelManager.Instance?.ProcessExplosion(transform.position);

        Debug.Log($"[BombProjectile] Exploding at {transform.position}. Spawning VFX: {(explosionVFXPrefab != null ? explosionVFXPrefab.name : "NULL")}");
        if (explosionVFXPrefab != null)
        {
            ObjectPoolManager.SpawnObject(explosionVFXPrefab, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, damageableLayers);
        Debug.Log($"[BombProjectile] Found {hits.Length} hits in radius {explosionRadius} on damageableLayers {damageableLayers.value}");
        foreach (Collider2D hit in hits)
        {
            // Do not damage the entity that threw the bomb
            if (owner != null && hit.transform.root.gameObject == owner.transform.root.gameObject) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            Debug.Log($"[BombProjectile] Hit {hit.gameObject.name}, IDamageable found: {damageable != null}");
            if (damageable != null && !damageable.IsDead)
            {
                // Calculate knockback strictly away from the bomb
                Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                
                // Add a slight upward bump to simulate being lifted by the explosion
                knockDir.y += 0.5f;
                knockDir.Normalize();

                DamageInfo info = DamageInfo.CreateWithKnockback(
                    runtimeExplosionDamage,
                    owner != null ? owner : gameObject, 
                    knockDir, 
                    runtimeKnockbackForce
                );
                info.damageSource = DamageSourceType.BombAttack;
                
                Debug.Log($"[BombProjectile] Dealing {runtimeExplosionDamage} damage to {hit.gameObject.name}");
                damageable.TakeDamage(info);
            }
        }

        if (fuseCoroutine != null)
        {
            StopCoroutine(fuseCoroutine);
            fuseCoroutine = null;
        }

        ObjectPoolManager.ReturnObjectToPool(gameObject);
        Debug.Log("[BombProjectile] Returned to pool.");
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
