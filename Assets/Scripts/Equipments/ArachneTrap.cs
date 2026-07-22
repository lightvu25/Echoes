using UnityEngine;

public class ArachneTrap : MonoBehaviour
{
    [SerializeField] private float rootDuration = 3f;
    [SerializeField] private LayerMask enemyLayer;

    private Collider2D trapCollider;

    private void Awake()
    {
        trapCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (trapCollider != null) trapCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;

        var movement = collision.GetComponent<IEnemyMovement>();
        if (movement != null)
        {
            // Immediately disable collider to prevent double-triggering
            if (trapCollider != null) trapCollider.enabled = false;

            movement.ApplyRoot(rootDuration);
            ObjectPoolManager.ReturnObjectToPool(gameObject); // Cleanup
        }
    }
}
