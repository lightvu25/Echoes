using UnityEngine;

[RequireComponent(typeof(InteractableTrigger))]
public class ItemDrop : MonoBehaviour, IInteractable
{
    [Header("Aura System (Items)")]
    [SerializeField] private SpriteRenderer auraRenderer;
    [Tooltip("Index 0 = Tier 1, Index 1 = Tier 2, etc.")]
    [SerializeField] private Sprite[] tierAuras;

    [Header("Item Data")]
    [SerializeField] private ItemBaseData itemData;
    [SerializeField] private SpriteRenderer itemSpriteRenderer;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
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

    public void Initialize(Vector2 initialForce, ItemBaseData droppedItemData)
    {
        if (droppedItemData != null && !droppedItemData.name.Contains("(Clone)"))
        {
            if (droppedItemData is RelicData r)
            {
                var clone = Instantiate(r);
                clone.InitRuntime();
                droppedItemData = clone;
            }
            else if (droppedItemData is EchoData e)
            {
                var clone = Instantiate(e);
                clone.InitRuntime();
                droppedItemData = clone;
            }
        }

        itemData = droppedItemData;
        UpdateAura();
        
        if (rb != null)
        {
            rb.AddForce(initialForce, ForceMode2D.Impulse);
        }
    }

    private void UpdateAura()
    {
        if (itemData != null)
        {
            if (itemSpriteRenderer != null)
            {
                itemSpriteRenderer.sprite = itemData.itemIcon;
            }
            
            if (auraRenderer != null && tierAuras != null)
            {
                // Smart Casting Logic
                int calculatedTier = itemData.itemTier;
                if (itemData is RelicData relic)
                {
                    calculatedTier = (int)relic.Rarity + 1;
                }
                else if (itemData is EchoData echo)
                {
                    calculatedTier = echo.level;
                }
                
                int tierIndex = Mathf.Max(0, calculatedTier - 1);
                
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
        }
        else
        {
            if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
        }
    }

    public void Interact()
    {
        Collect();
    }

    private void Collect()
    {
        if (itemData != null && PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.TryEquip(itemData);
        }
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
