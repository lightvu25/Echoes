using System.Collections.Generic;
using UnityEngine;

public class ToxicCloud : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int tickDamage = 5;
    [SerializeField] private float tickRate = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    private readonly List<IDamageable> enemiesInCloud = new List<IDamageable>();
    private readonly Dictionary<IDamageable, int> colliderOccupancy = new Dictionary<IDamageable, int>();
    private float nextTickTime;
    private GameObject owner;

    public void Initialize(GameObject attacker)
    {
        StopAllCoroutines();
        owner = attacker;
        nextTickTime = Time.time + tickRate;
        StartCoroutine(LifetimeRoutine());
    }

    private void OnEnable()
    {
        Collider2D cloudCollider = GetComponent<Collider2D>();
        if (cloudCollider != null) cloudCollider.isTrigger = true;
        enemiesInCloud.Clear();
        colliderOccupancy.Clear();
        owner = null;
    }

    private void OnDisable()
    {
        enemiesInCloud.Clear();
        colliderOccupancy.Clear();
        StopAllCoroutines();
        owner = null;
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
            IDamageable target = enemiesInCloud[i];

            if (target == null || target.IsDead)
            {
                enemiesInCloud.RemoveAt(i);
                continue;
            }

            DamageInfo dInfo = DamageInfo.Create(tickDamage, owner != null ? owner : gameObject);
            dInfo.damageSource = DamageSourceType.Poison;
            target.TakeDamage(dInfo);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        IDamageable target = collision.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            colliderOccupancy.TryGetValue(target, out int count);
            colliderOccupancy[target] = count + 1;
            if (count == 0) enemiesInCloud.Add(target);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        IDamageable target = collision.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            if (!colliderOccupancy.TryGetValue(target, out int count)) return;
            if (count <= 1)
            {
                colliderOccupancy.Remove(target);
                enemiesInCloud.Remove(target);
            }
            else
            {
                colliderOccupancy[target] = count - 1;
            }
        }
    }
}
