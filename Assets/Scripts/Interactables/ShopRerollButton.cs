using UnityEngine;
using TMPro;

public class ShopRerollButton : ShopInteractableBase
{
    [SerializeField] private ShopRoomManager roomManager;
    [SerializeField] private TextMeshPro costText;
    [SerializeField] private AudioClip rerollSFX;
    [SerializeField] private AudioClip errorSFX;

    [SerializeField] private int baseCost = 10;
    [SerializeField] private int costIncrease = 10;

    private int currentCost;

    private void Start()
    {
        currentCost = baseCost;
        UpdateText();
    }

    protected override void DoInteract()
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.SpendGold(currentCost))
        {
            PlayClip(rerollSFX);
            if (roomManager != null) roomManager.RerollShop();
            currentCost += costIncrease;
            UpdateText();
        }
        else
        {
            PlayClip(errorSFX);
            if (costText != null) StartCoroutine(FlashTextColor(costText, Color.red, 0.2f));
        }
    }

    private void UpdateText()
    {
        if (costText != null) costText.text = currentCost.ToString();
    }
}
