using UnityEngine;

public class Collectible : MonoBehaviour
{
    public enum CollectibleType
    {
        Gold,
        AstralShard,
        Item // Consolidates Relic, Memory, and Consumable
    }

    [Header("Collectible Settings")]
    [SerializeField] private CollectibleType type;
    [SerializeField] private float magnetAcceleration;
    [SerializeField] private float magnetDelay;
    [SerializeField] private GameObject damagePopupPrefab;

    [Header("Visual Tiers (Gold/Shards)")]
    [SerializeField] private GameObject[] visualTiers;
    [SerializeField] private int[] valueThresholds;
    
    [Header("Aura System (Items)")]
    [SerializeField] private SpriteRenderer auraRenderer;
    [Tooltip("Index 0 = Tier 1, Index 1 = Tier 2, etc.")]
    [SerializeField] private Sprite[] tierAuras;

    [Header("Item Data (If type is Item)")]
    [SerializeField] private ItemBaseData itemData;

    private int amount;
    private bool isMagnetized = false;
    private Transform targetPlayer;
    private Rigidbody2D rb;
    private bool canMagnetize = false;
    private float magnetTimer = 0f;
    private float currentMagnetSpeed = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        isMagnetized = false;
        targetPlayer = null;
        canMagnetize = false;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        Collider2D[] myCols = GetComponents<Collider2D>();
        foreach (var myCol in myCols)
        {
            if (myCol != null)
            {
                myCol.excludeLayers = ~LayerMask.GetMask("Ground", "Wall");
            }
        }
    }

    public void Initialize(int amount, Vector2 initialForce)
    {
        this.amount = amount;
        magnetTimer = magnetDelay;
        canMagnetize = false;
        currentMagnetSpeed = 0f;
        
        UpdateVisuals(amount);
        UpdateAura();
        
        if (rb != null)
        {
            rb.AddForce(initialForce, ForceMode2D.Impulse);
        }
    }

    private void UpdateVisuals(int val)
    {
        // Only Gold and AstralShard use value thresholds to change their sprite
        if (type == CollectibleType.Item) return;
        
        if (visualTiers == null || valueThresholds == null || visualTiers.Length == 0) return;
        
        int tierLevels = Mathf.Min(visualTiers.Length, valueThresholds.Length);
        int activeTierIndex = 0;
        
        for (int i = 0; i < tierLevels; i++)
        {
            if (val >= valueThresholds[i])
            {
                activeTierIndex = i;
            }
            else
            {
                break;
            }
        }
        
        for (int i = 0; i < visualTiers.Length; i++)
        {
            if (visualTiers[i] != null)
                visualTiers[i].SetActive(i == activeTierIndex);
        }
    }

    private void UpdateAura()
    {
        if (auraRenderer == null) return;
        
        if (type == CollectibleType.Item && itemData != null && tierAuras != null)
        {
            // Tiers usually start at 1, so index is tier - 1
            int tierIndex = Mathf.Max(0, itemData.itemTier - 1);
            
            if (tierIndex < tierAuras.Length)
            {
                auraRenderer.sprite = tierAuras[tierIndex];
                auraRenderer.gameObject.SetActive(tierAuras[tierIndex] != null);
            }
            else
            {
                auraRenderer.gameObject.SetActive(false);
            }
        }
        else
        {
            // Non-tier items or missing data get no aura
            auraRenderer.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isMagnetized) return;
        if (!other.CompareTag("Player")) return;
        
        // Allow immediate manual pickup on collision if it hasn't magnetized yet
        Collect();
    }

    private void Update()
    {
        if (!canMagnetize)
        {
            magnetTimer -= Time.deltaTime;
        
            if (magnetTimer <= 0f)
            {
                canMagnetize = true;
                
                // Track player collection point for magnet
                if (PlayerStats.Instance != null && PlayerStats.Instance.collectPoint != null)
                {
                    targetPlayer = PlayerStats.Instance.collectPoint;
                }
                else if (PlayerInventoryCore.Instance != null)
                {
                    targetPlayer = PlayerInventoryCore.Instance.transform;
                }
                
                if (targetPlayer != null)
                {
                    isMagnetized = true;
                    currentMagnetSpeed = 0f;
                    
                    Collider2D[] cols = GetComponents<Collider2D>();
                    foreach (Collider2D col in cols)
                    {
                        col.enabled = false;
                    }
                    
                    if (rb != null)
                    {
                        rb.bodyType = RigidbodyType2D.Kinematic;
                        rb.linearVelocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }
                }
            }
            return;
        }
        
        if (targetPlayer != null && isMagnetized)
        {
            Vector3 targetPosition = targetPlayer.position;
            currentMagnetSpeed += magnetAcceleration * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentMagnetSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
            {
                Collect();
            }
        }
    }

    private void Collect()
    {
        switch (type)
        {
            case CollectibleType.Gold:
                if (PlayerStats.Instance != null) PlayerStats.Instance.AddGold(amount);
                break;
            case CollectibleType.AstralShard:
                if (PlayerStats.Instance != null) PlayerStats.Instance.AddAstralShards(amount);
                break;
            case CollectibleType.Item:
                if (itemData != null && PlayerInventoryCore.Instance != null)
                {
                    PlayerInventoryCore.Instance.TryEquip(itemData);
                }
                break;
        }
        
        if (damagePopupPrefab != null && type != CollectibleType.Item)
        {
            GameObject popup = ObjectPoolManager.SpawnObject(damagePopupPrefab, transform.position, Quaternion.identity);
            if (popup.TryGetComponent<DamagePopup>(out var dp))
            {
                dp.Setup(amount);
            }
        }
        
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}