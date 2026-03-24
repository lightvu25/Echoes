using System;
using UnityEngine;

public class ShopItem
{
    public string label;
    public int coinCost;
    public Action onPurchase;
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    public event Action<ShopItem[]> OnShopOpened;

    private ShopItem[] currentShopItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenShop()
    {
        currentShopItems = new ShopItem[]
        {
            new ShopItem
            {
                label = "Buy Healing (+50 HP)",
                coinCost = 20,
                onPurchase = () => BuyHealing(50)
            },
            new ShopItem
            {
                label = "Buy Max HP Slot (+25 Max HP)",
                coinCost = 100,
                onPurchase = () => BuyMaxHPSlot(25)
            }
        };

        OnShopOpened?.Invoke(currentShopItems);
    }

    public void TryPurchase(int index)
    {
        if (currentShopItems == null || index < 0 || index >= currentShopItems.Length) return;

        ShopItem item = currentShopItems[index];
        if (PlayerStats.Instance != null && PlayerStats.Instance.SpendGold(item.coinCost))
        {
            item.onPurchase?.Invoke();
            Debug.Log($"ShopManager: Purchased {item.label}");
        }
        else
        {
            Debug.LogWarning("ShopManager: Not enough gold to purchase!");
        }
    }

    private void BuyHealing(int amount)
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.TryGetComponent(out HealthSystem healthSys))
        {
            healthSys.Heal(amount);
        }
    }

    private void BuyMaxHPSlot(int hpGain)
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.TryGetComponent(out HealthSystem healthSys))
        {
            healthSys.SetMaxHP(healthSys.MaxHP + hpGain, true); 
        }
    }
}
