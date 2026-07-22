using System.Collections.Generic;
using UnityEngine;

public class CrimsonDart : MonoBehaviour
{
    [SerializeField] private int baseDamage = 15;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask enemyLayer;

    private GameObject owner;
    private HashSet<int> hitEnemies = new HashSet<int>();

    public void Initialize(GameObject attacker)
    {
        owner = attacker;
    }

    private void OnEnable()
    {
        StartCoroutine(LifetimeRoutine());
    }

    private void OnDisable()
    {
        hitEnemies.Clear();
    }

    private System.Collections.IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Early out using LayerMask bitwise check
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        int instanceID = collision.gameObject.GetInstanceID();

        // Ensure we only damage each enemy exactly once while piercing
        if (hitEnemies.Contains(instanceID)) return;
        hitEnemies.Add(instanceID);

        var health = collision.GetComponent<HealthSystem>();
        if (health != null && !health.IsDead)
        {
            DamageInfo dInfo = DamageInfo.Create(baseDamage, owner != null ? owner : gameObject);
            health.TakeDamage(dInfo);
        }
    }
}
