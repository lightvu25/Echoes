using UnityEngine;

public class ToxicFlask : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    private GameObject owner;
    private GameObject cloudPrefab;

    public void Initialize(GameObject attacker, GameObject cloudPrefabRef)
    {
        StopAllCoroutines();
        owner = attacker;
        cloudPrefab = cloudPrefabRef;
        StartCoroutine(LifetimeRoutine());
    }

    private void OnEnable()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null) body.gravityScale = 1f;
        owner = null;
        cloudPrefab = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        owner = null;
        cloudPrefab = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (cloudPrefab != null)
        {
            GameObject cloud = ObjectPoolManager.SpawnObject(cloudPrefab, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.Projectile);
            var toxicCloud = cloud.GetComponent<ToxicCloud>();
            if (toxicCloud != null)
            {
                toxicCloud.Initialize(owner);
            }
        }

        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }

    private System.Collections.IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
