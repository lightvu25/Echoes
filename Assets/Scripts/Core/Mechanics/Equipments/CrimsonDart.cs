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
        StopAllCoroutines();
        owner = attacker;
        StartCoroutine(LifetimeRoutine());
    }

    private void OnEnable()
    {
        owner = null;
        hitEnemies.Clear();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        hitEnemies.Clear();
        owner = null;
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

        IDamageable target = collision.GetComponentInParent<IDamageable>();
        if (target == null || target.IsDead) return;
        int instanceID = target.Transform.GetInstanceID();

        // Ensure we only damage each enemy exactly once while piercing
        if (hitEnemies.Contains(instanceID)) return;
        hitEnemies.Add(instanceID);

        DamageInfo dInfo = DamageInfo.Create(baseDamage, owner != null ? owner : gameObject);
        dInfo.damageSource = DamageSourceType.Equipment;
        dInfo.isPiercing = true;
        target.TakeDamage(dInfo);
    }
}
