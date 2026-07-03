using UnityEngine;
using System.Collections.Generic;

public class BlackHoleEffect : MonoBehaviour
{
    [Header("Black Hole Stats")]
    public float duration = 4f;
    public float pullRadius = 5f;
    public float pullForce = 15f;
    public int damagePerTick = 10;
    public float tickRate = 0.5f;

    private float lifeTimer;
    private float tickTimer;
    private LayerMask enemyLayer;
    private GameObject owner;

    public void Initialize(GameObject spawner)
    {
        owner = spawner;
        lifeTimer = duration;
        enemyLayer = LayerMask.GetMask("Enemy"); // Ensure this matches the project's layer
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0) Destroy(gameObject);

        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0)
        {
            DealDamageTick();
            tickTimer = tickRate;
        }
    }

    private void FixedUpdate()
    {
        // Gravitational Pull Logic
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pullRadius, enemyLayer);
        foreach (Collider2D col in colliders)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = (transform.position - col.transform.position).normalized;
                rb.AddForce(direction * pullForce);
            }
        }
    }

    private void DealDamageTick()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pullRadius, enemyLayer);
        foreach (Collider2D col in colliders)
        {
            IDamageable target = col.GetComponent<IDamageable>();
            if (target != null)
            {
                DamageInfo tickDamage = DamageInfo.Create(damagePerTick, owner);
                tickDamage.isTrueDamage = true;
                tickDamage.damageSource = "BlackHole";
                target.TakeDamage(tickDamage);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }
}
