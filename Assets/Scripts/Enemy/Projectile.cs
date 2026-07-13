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
    
    [Header("Damage Scaling")]
    [SerializeField] private bool scaleDamageWithSpeed = false;
    [Tooltip("How much damage multiplier is added per 1 unit of speed. 0.05 = 5% extra damage per 1 speed.")]
    [SerializeField] private float speedDamageMultiplier = 0.05f;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private bool hasHit;
    private Coroutine lifetimeCoroutine;
    private GameObject owner;
    
    private DamageInfo? overrideDamageInfo = null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
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

    public void SetupPlayerProjectile(DamageInfo info, Vector2 velocity)
    {
        overrideDamageInfo = info;
        owner = info.attacker;
        
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }

    private void Update()
    {
        if (hasHit || rb == null) return;
        
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Prevent the sprite from rendering upside-down when travelling left
            if (sr != null)
            {
                if (Mathf.Abs(angle) > 90f)
                {
                    sr.flipY = true;
                }
                else
                {
                    sr.flipY = false;
                }
            }
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        // Ignore the thrower
        if (owner != null && (other.gameObject == owner || other.transform.root.gameObject == owner)) return;

        bool isHitLayer = (hitLayers.value & (1 << other.gameObject.layer)) != 0;
        bool isEnvironment = other.gameObject.layer == LayerMask.NameToLayer("Ground") || 
                             other.gameObject.layer == LayerMask.NameToLayer("Wall");

        // If it's not a target and not a wall, ignore it (e.g. background elements, triggers, etc)
        if (!isHitLayer && !isEnvironment) return;

        // If it's a valid target, try to apply damage
        if (isHitLayer)
        {
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
            {
                DamageInfo info;
                Vector2 knockDir = (other.transform.position - transform.position).normalized;
                
                if (overrideDamageInfo.HasValue)
                {
                    info = overrideDamageInfo.Value;
                    info.knockbackDirection = knockDir;
                }
                else
                {
                    info = DamageInfo.CreateWithKnockback(damage, owner, knockDir, knockbackForce);
                    info.damageSource = "Projectile";
                }
                
                if (scaleDamageWithSpeed && rb != null)
                {
                    float currentSpeed = rb.linearVelocity.magnitude;
                    // Example: 20 speed * 0.05 = +1.0 (double damage)
                    info.multiplicativeStack *= (1f + (currentSpeed * speedDamageMultiplier));
                }
                
                target.TakeDamage(info);
            }
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
