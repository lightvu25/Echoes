using UnityEngine;

public class ToxicFlask : MonoBehaviour
{
    private GameObject owner;
    private GameObject cloudPrefab;

    public void Initialize(GameObject attacker, GameObject cloudPrefabRef)
    {
        owner = attacker;
        cloudPrefab = cloudPrefabRef;
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
}
