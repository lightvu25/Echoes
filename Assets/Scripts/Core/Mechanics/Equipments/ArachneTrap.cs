using UnityEngine;

public class ArachneTrap : MonoBehaviour
{
    [SerializeField] private float rootDuration = 3f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float lifetime = 15f;

    private Collider2D trapCollider;

    private void Awake()
    {
        trapCollider = GetComponent<Collider2D>();
        if (trapCollider != null) trapCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        if (trapCollider != null) trapCollider.enabled = true;
        StartCoroutine(LifetimeRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        var movement = collision.GetComponentInParent<IEnemyMovement>();
        if (movement != null)
        {
            // Immediately disable collider to prevent double-triggering
            if (trapCollider != null) trapCollider.enabled = false;

            movement.ApplyRoot(rootDuration);
            ObjectPoolManager.ReturnObjectToPool(gameObject); // Cleanup
        }
    }

    private System.Collections.IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
