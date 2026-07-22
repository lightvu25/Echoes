using System.Collections.Generic;
using UnityEngine;

public class ToxicCloud : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int tickDamage = 5;
    [SerializeField] private float tickRate = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    private List<HealthSystem> enemiesInCloud = new List<HealthSystem>();
    private float nextTickTime;
    private GameObject owner;

    public void Initialize(GameObject attacker)
    {
        owner = attacker;
    }

    private void OnEnable()
    {
        nextTickTime = Time.time + tickRate;
        StartCoroutine(LifetimeRoutine());
    }

    private void OnDisable()
    {
        enemiesInCloud.Clear();
    }

    private System.Collections.IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }

    private void Update()
    {
        if (Time.time >= nextTickTime)
        {
            ApplyTickDamage();
            nextTickTime = Time.time + tickRate;
        }
    }

    private void ApplyTickDamage()
    {
        // Iterate backwards so we can safely remove dead/null enemies during traversal
        for (int i = enemiesInCloud.Count - 1; i >= 0; i--)
        {
            HealthSystem health = enemiesInCloud[i];

            if (health == null || health.IsDead)
            {
                enemiesInCloud.RemoveAt(i);
                continue;
            }

            DamageInfo dInfo = DamageInfo.Create(tickDamage, owner != null ? owner : gameObject);
            health.TakeDamage(dInfo);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        var health = collision.GetComponent<HealthSystem>();
        if (health != null && !enemiesInCloud.Contains(health))
        {
            enemiesInCloud.Add(health);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        var health = collision.GetComponent<HealthSystem>();
        if (health != null)
        {
            enemiesInCloud.Remove(health);
        }
    }
}
