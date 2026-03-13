using UnityEngine;

public class Collectible : MonoBehaviour
{
    public enum CollectibleType
    {
        Gold,
        MemoryFragment
    }

    [Header("Collectible Settings")]
    [SerializeField] private CollectibleType type;
    [SerializeField] private float magnetAcceleration;
    [SerializeField] private float magnetDelay;
    [SerializeField] private GameObject damagePopupPrefab;

    [Header("Visual Tiers")]
    [SerializeField] private GameObject[] visualTiers;
    [SerializeField] private int[] valueThresholds;

    private int amount;
    private bool isMagnetized = false;
    private PlayerStats targetPlayer;
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
        
        if (rb != null)
        {
            rb.AddForce(initialForce, ForceMode2D.Impulse);
        }
    }

    private void UpdateVisuals(int val)
    {
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
            visualTiers[i].SetActive(i == activeTierIndex);
        }
    }

    private void Update()
    {
        if (!canMagnetize)
        {
            magnetTimer -= Time.deltaTime;
        
            if (magnetTimer <= 0f)
            {
                canMagnetize = true;

                targetPlayer = FindFirstObjectByType<PlayerStats>();
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
            Vector3 targetPosition = targetPlayer.collectPoint != null ? targetPlayer.collectPoint.position : targetPlayer.transform.position;

            currentMagnetSpeed += magnetAcceleration * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentMagnetSpeed * Time.deltaTime);

            Collect(targetPlayer);
        }
    }

    private void Collect(PlayerStats player)
    {
        switch (type)
        {
            case CollectibleType.Gold:
                player.AddGold(amount);
                break;
            case CollectibleType.MemoryFragment:
                player.AddMemoryFragments(amount);
                break;
        }

        if (damagePopupPrefab != null)
        {
            GameObject popup = ObjectPoolManager.SpawnObject(damagePopupPrefab, transform.position, Quaternion.identity);
            DamagePopup dp = popup.GetComponent<DamagePopup>();
            if (dp != null)
            {
                dp.Setup(amount);
            }
        }

        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}