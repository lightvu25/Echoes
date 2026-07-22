using UnityEngine;

public class CrimsonFlower : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private int maxDrops = 3;
    [SerializeField] private GameObject crimsonOrbPrefab;
    [SerializeField] private Sprite depletedSprite;
    
    private int dropsRemaining;
    private bool isDepleted = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    public bool IsDead => isDepleted;
    public Transform Transform => transform;
    public float Defense => 0f;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        dropsRemaining = maxDrops;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDepleted) return;

        if (dropsRemaining > 0)
        {
            dropsRemaining--;
            SpawnOrb();
            
            if (GameFeelManager.Instance != null && damageInfo.attacker != null)
            {
                GameFeelManager.Instance.ProcessHit(damageInfo.attacker, gameObject, 0, false);
            }
            
            if (dropsRemaining <= 0)
            {
                Deplete();
            }
        }
    }

    private void SpawnOrb()
    {
        if (crimsonOrbPrefab != null)
        {
            GameObject orb = ObjectPoolManager.SpawnObject(crimsonOrbPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity, ObjectPoolManager.PoolType.Loot);
            ResourceDrop resourceDrop = orb.GetComponent<ResourceDrop>();
            if (resourceDrop != null)
            {
                float randomX = UnityEngine.Random.Range(-5f, 5f);
                float randomY = UnityEngine.Random.Range(5f, 10f);
                resourceDrop.Initialize(1, new Vector2(randomX, randomY));
            }
        }
    }

    private void Deplete()
    {
        isDepleted = true;
        
        if (spriteRenderer != null && depletedSprite != null)
        {
            spriteRenderer.sprite = depletedSprite;
        }
        
        if (col != null)
        {
            col.enabled = false;
        }
    }
}
