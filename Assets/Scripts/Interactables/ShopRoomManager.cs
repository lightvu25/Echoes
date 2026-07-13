using UnityEngine;

public class ShopRoomManager : MonoBehaviour
{
    [Header("Pedestals (0: Item, 1: Relic, 2: Echo)")]
    [SerializeField] private ShopPedestal[] pedestals = new ShopPedestal[3];

    [SerializeField] private int minTierToSpawn = 2;

    private void Start()
    {
        GenerateShop();
    }

    /// <summary>Clears existing items and populates pedestals with filtered, high-tier drops from the Global ShopManager.</summary>
    public void GenerateShop()
    {
        if (pedestals.Length < 3) return;

        if (ShopManager.Instance == null)
        {
            Debug.LogWarning("[ShopRoomManager] No ShopManager instance found! Please place a ShopManager in the scene.");
            return;
        }

        ItemBaseData consumable = ShopManager.Instance.GetRandomItem(ItemCategory.Item, minTierToSpawn);
        ItemBaseData relic = ShopManager.Instance.GetRandomItem(ItemCategory.Relic, minTierToSpawn);
        ItemBaseData echo = ShopManager.Instance.GetRandomItem(ItemCategory.Echo, minTierToSpawn);

        if (pedestals[0] != null) pedestals[0].Setup(consumable);
        if (pedestals[1] != null) pedestals[1].Setup(relic);
        if (pedestals[2] != null) pedestals[2].Setup(echo);
    }

    public void RerollShop()
    {
        GenerateShop();
    }
}
