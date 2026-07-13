using UnityEngine;
using TMPro;

public class ShopPedestal : ShopInteractableBase
{
    [SerializeField] private SpriteRenderer itemSpriteRenderer;
    [SerializeField] private SpriteRenderer glowAura;
    [SerializeField] private TextMeshPro priceText;
    [SerializeField] private ParticleSystem purchaseVFX;
    [SerializeField] private AudioClip purchaseSFX;
    [SerializeField] private AudioClip errorSFX;

    private ItemBaseData currentItem;
    private int currentPrice;

    /// <summary>Configures the pedestal visuals and calculates the price based on item tier.</summary>
    public void Setup(ItemBaseData item)
    {
        if (item == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        currentItem = item;
        currentPrice = item.basePrice * item.itemTier;

        if (itemSpriteRenderer != null) itemSpriteRenderer.sprite = item.itemIcon;
        if (priceText != null) priceText.text = currentPrice.ToString();
        
        SetAuraColor(item.itemTier);
    }

    protected override void DoInteract()
    {
        if (currentItem == null) return;

        if (PlayerStats.Instance != null && PlayerStats.Instance.SpendGold(currentPrice))
        {
            PlayerInventoryCore.Instance.TryEquip(currentItem);
            
            if (purchaseVFX != null) purchaseVFX.Play();
            PlayClip(purchaseSFX);
            
            currentItem = null;
            if (itemSpriteRenderer != null) itemSpriteRenderer.sprite = null;
            if (glowAura != null) glowAura.gameObject.SetActive(false);
            if (priceText != null) priceText.text = string.Empty;
            
            gameObject.SetActive(false);
        }
        else
        {
            PlayClip(errorSFX);
            if (priceText != null) StartCoroutine(FlashTextColor(priceText, Color.red, 0.2f));
        }
    }

    private void SetAuraColor(int tier)
    {
        if (glowAura == null) return;
        
        glowAura.gameObject.SetActive(true);
        switch (tier)
        {
            case 1: glowAura.color = Color.white; break;
            case 2: glowAura.color = Color.cyan; break;
            case 3: glowAura.color = Color.magenta; break;
            default: glowAura.color = Color.yellow; break;
        }
    }
}
