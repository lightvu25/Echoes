using System.Collections;
using UnityEngine;
using TMPro;

public class ShopPedestal : ShopInteractableBase
{
    [SerializeField] private SpriteRenderer itemSpriteRenderer;
    
    [Header("Aura System (Items)")]
    [SerializeField] private SpriteRenderer auraRenderer;
    [Tooltip("Index 0 = Tier 1, Index 1 = Tier 2, etc.")]
    [SerializeField] private Sprite[] tierAuras;
    
    [Header("Price Popup")]
    [SerializeField] private GameObject priceTextPrefab;
    [SerializeField] private Vector3 priceTextOffset = new Vector3(0f, -1.25f, 0f);

    [Header("Audio")]
    [SerializeField] private AudioClip purchaseSFX;
    [SerializeField] private AudioClip errorSFX;

    private TextMeshPro priceText;
    private ItemBaseData currentItem;
    private int currentPrice;
    private Coroutine popupCoroutine;

    /// <summary>Configures the pedestal visuals and calculates the price based on item tier.</summary>
    public void Setup(ItemBaseData item, float inflationMultiplier = 1f)
    {
        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (item != null && !item.name.Contains("(Clone)"))
        {
            if (item is RelicData r)
            {
                var clone = Instantiate(r);
                clone.InitRuntime();
                item = clone;
            }
            else if (item is EchoData e)
            {
                var clone = Instantiate(e);
                clone.InitRuntime();
                item = clone;
            }
        }

        currentItem = item;

        // Smart Casting Logic
        int calculatedTier = item.itemTier;
        if (item is RelicData relic)
        {
            calculatedTier = (int)relic.Rarity + 1;
        }
        else if (item is EchoData echo)
        {
            calculatedTier = echo.level;
        }

        float rawPrice = (item.basePrice * calculatedTier) * inflationMultiplier;
        currentPrice = Mathf.RoundToInt(rawPrice);

        if (itemSpriteRenderer != null) itemSpriteRenderer.sprite = item.itemIcon;
        
        // Spawn the Price Text prefab if it doesn't exist yet
        if (priceText == null && priceTextPrefab != null)
        {
            GameObject obj = Instantiate(priceTextPrefab, transform);
            obj.transform.localPosition = priceTextOffset;
            priceText = obj.GetComponent<TextMeshPro>();
            if (priceText == null) priceText = obj.GetComponentInChildren<TextMeshPro>();
        }

        if (priceText != null) 
        {
            priceText.text = currentPrice.ToString();
            priceText.sortingLayerID = SortingLayer.NameToID("UI");
            priceText.sortingOrder = 10;
            priceText.gameObject.SetActive(false);
        }
        
        SetAuraColor(calculatedTier);
    }

    protected override void DoInteract()
    {
        if (currentItem == null) return;

        if (PlayerStats.Instance != null && PlayerStats.Instance.SpendGold(currentPrice))
        {
            PlayerInventoryCore.Instance.TryEquip(currentItem);
            
            PlayClip(purchaseSFX);
            
            currentItem = null;
            if (itemSpriteRenderer != null) itemSpriteRenderer.sprite = null;
            if (auraRenderer != null) auraRenderer.gameObject.SetActive(false);
            if (priceText != null) priceText.text = string.Empty;
            
            gameObject.SetActive(false);
        }
        else
        {
            PlayClip(errorSFX);
            if (priceText != null) StartCoroutine(FlashTextColor(priceText, Color.red, 0.2f));
            if (itemSpriteRenderer != null) StartCoroutine(FlashSpriteColor(itemSpriteRenderer, Color.red, 0.2f));
        }
    }

    private void SetAuraColor(int tier)
    {
        if (auraRenderer != null && tierAuras != null)
        {
            int tierIndex = Mathf.Max(0, tier - 1);
            
            if (tierIndex < tierAuras.Length)
            {
                auraRenderer.sprite = tierAuras[tierIndex];
                auraRenderer.color = Color.white; // Reset color in case it was modified before
                auraRenderer.gameObject.SetActive(tierAuras[tierIndex] != null);
            }
            else
            {
                auraRenderer.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        // Spin the aura ring just like in ItemDrop.cs
        if (auraRenderer != null && auraRenderer.gameObject.activeSelf)
        {
            auraRenderer.transform.Rotate(Vector3.forward, -30f * Time.deltaTime);
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        
        if (other.CompareTag("Player") && currentItem != null && priceText != null)
        {
            Debug.Log($"[ShopPedestal] Player entered trigger! Item: {currentItem.name}, Price: {currentPrice}");
            if (popupCoroutine != null) StopCoroutine(popupCoroutine);
            popupCoroutine = StartCoroutine(AnimatePopup(true));
        }
        else if (other.CompareTag("Player"))
        {
            Debug.Log($"[ShopPedestal] Player entered trigger, but something is null! currentItem null? {currentItem == null}, priceText null? {priceText == null}");
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);
        if (other.CompareTag("Player") && priceText != null)
        {
            Debug.Log("[ShopPedestal] Player exited trigger!");
            if (popupCoroutine != null) StopCoroutine(popupCoroutine);
            
            // Only start the coroutine if the GameObject is still active.
            // If the player just bought the item, gameObject is set to false, 
            // which automatically triggers OnTriggerExit2D, and we shouldn't animate.
            if (gameObject.activeInHierarchy)
            {
                popupCoroutine = StartCoroutine(AnimatePopup(false));
            }
        }
    }

    private IEnumerator AnimatePopup(bool show)
    {
        Debug.Log($"[ShopPedestal] AnimatePopup({show}) started. priceText was active: {priceText.gameObject.activeSelf}");
        if (show)
        {
            priceText.gameObject.SetActive(true);
            priceText.transform.localScale = Vector3.zero;
        }

        Vector3 targetScale = show ? Vector3.one : Vector3.zero;
        float speed = 10f;

        while (Vector3.Distance(priceText.transform.localScale, targetScale) > 0.01f)
        {
            priceText.transform.localScale = Vector3.Lerp(priceText.transform.localScale, targetScale, Time.deltaTime * speed);
            yield return null;
        }

        priceText.transform.localScale = targetScale;
        
        if (!show)
        {
            priceText.gameObject.SetActive(false);
        }
    }
}
