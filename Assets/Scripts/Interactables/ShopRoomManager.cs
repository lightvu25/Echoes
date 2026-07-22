using UnityEngine;

public class ShopRoomManager : MonoBehaviour
{
    [Header("Economy Settings")]
    [Tooltip("Price increase per level. 0.2 = 20% increase per depth level.")]
    [SerializeField] private float inflationPerLevel = 0.2f;

    [Header("Pedestals (0: Tool, 1: Relic, 2: Echo)")]
    [SerializeField] private ShopPedestal[] pedestals = new ShopPedestal[3];

    [SerializeField] private int minTierToSpawn = 1;

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

        ItemBaseData tool = ShopManager.Instance.GetRandomItem(ItemCategory.Tool, minTierToSpawn);
        ItemBaseData relic = ShopManager.Instance.GetRandomItem(ItemCategory.Relic, minTierToSpawn);
        ItemBaseData echo = ShopManager.Instance.GetRandomItem(ItemCategory.Echo, minTierToSpawn);

        int currentLevel = GameManager.Instance != null ? GameManager.Instance.GetLevelNumber() : 1;
        float currentInflationMultiplier = 1f + ((currentLevel - 1) * inflationPerLevel);

        if (pedestals[0] != null) pedestals[0].Setup(tool, currentInflationMultiplier);
        if (pedestals[1] != null) pedestals[1].Setup(relic, currentInflationMultiplier);
        if (pedestals[2] != null) pedestals[2].Setup(echo, currentInflationMultiplier);
    }

    public void RerollShop()
    {
        GenerateShop();
    }
}
