using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SacrificialUI : MonoBehaviour, IUIPanel
{
    [Header("Inventory Display")]
    [SerializeField] private Transform inventoryGrid;
    [UnityEngine.Serialization.FormerlySerializedAs("draggableElement")]
    [SerializeField] private GameObject draggableEchoPrefab;

    [Header("Fusion Slots")]
    [SerializeField] private FusionSlot slotA;
    [SerializeField] private FusionSlot slotB;

    [Header("Result Slot")]
    [SerializeField] private Image result;

    [Header("Recipes")]
    [SerializeField] private List<FusionRecipeData> recipes = new List<FusionRecipeData>();
    
    [Header("UI Feedback")]
    [SerializeField] private Button forgeButton;

    [Header("Panel")]
    [SerializeField] private UIPanelAnimator _panelAnimator;

    private FusionRecipeData currentValidRecipe;
    private bool isOpen = false;

    private void Awake()
    {
        if (forgeButton != null)
        {
            forgeButton.onClick.RemoveAllListeners();
            forgeButton.onClick.AddListener(ExecuteFusion);
        }
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnCancelPressed += HandleCancelPressed;
    }

    private void OnDisable()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnCancelPressed -= HandleCancelPressed;
    }

    private void HandleCancelPressed()
    {
        if (isOpen && UIManager.Instance != null)
            UIManager.Instance.CloseCurrentPanel();
    }

    public void Show()
    {
        isOpen = true;
        if (_panelAnimator != null) _panelAnimator.Show(); else gameObject.SetActive(true);
        currentValidRecipe = null;
        if (forgeButton != null) forgeButton.interactable = false;
        
        if (slotA != null) slotA.ClearSlot();
        if (slotB != null) slotB.ClearSlot();
        
        if (result != null) result.enabled = false;

        PopulateInventory();

        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
    }

    private void PopulateInventory()
    {
        if (inventoryGrid == null || draggableEchoPrefab == null || PlayerInventoryCore.Instance == null) return;

        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }

        IReadOnlyList<ItemBaseData> echoes = PlayerInventoryCore.Instance.GetEquippedList(ItemCategory.Echo);
        int maxSlots = PlayerInventoryCore.Instance.UnlockedEchoSlots;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject go = Instantiate(draggableEchoPrefab, inventoryGrid);
            DraggableEcho dragEcho = go.GetComponent<DraggableEcho>();
            
            if (dragEcho != null)
            {
                if (echoes != null && i < echoes.Count)
                {
                    EchoData echoData = echoes[i] as EchoData;
                    dragEcho.Setup(echoData);
                }
                else
                {
                    dragEcho.Setup(null);
                }
            }
        }
    }

    public void Hide()
    {
        isOpen = false;
        
        if (slotA != null) slotA.ReturnItem();
        if (slotB != null) slotB.ReturnItem();
        
        currentValidRecipe = null;
        if (_panelAnimator != null) _panelAnimator.Hide(); else gameObject.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    public void CheckRecipe()
    {
        currentValidRecipe = null;
        if (forgeButton != null) forgeButton.interactable = false;

        if (slotA.SlottedEcho == null || slotB.SlottedEcho == null)
        {
            if (result != null) result.enabled = false;
            return;
        }

        foreach (var recipe in recipes)
        {
            if (recipe == null || recipe.echoA == null || recipe.echoB == null) continue;

            if ((recipe.echoA.itemID == slotA.SlottedEcho.itemID && recipe.echoB.itemID == slotB.SlottedEcho.itemID) ||
                (recipe.echoA.itemID == slotB.SlottedEcho.itemID && recipe.echoB.itemID == slotA.SlottedEcho.itemID))
            {
                currentValidRecipe = recipe;
                break;
            }
        }

        if (currentValidRecipe != null)
        {
            if (result != null)
            {
                if (currentValidRecipe.resultEcho != null)
                {
                    result.sprite = currentValidRecipe.resultEcho.itemIcon;
                    result.enabled = true;
                }
                else
                {
                    result.enabled = false;
                }
            }

            bool hasNode = string.IsNullOrEmpty(currentValidRecipe.requiredConstellationNode) || 
                           (GameSession.Instance != null && GameSession.Instance.currentProfile != null && GameSession.Instance.currentProfile.HasSkill(currentValidRecipe.requiredConstellationNode));

            if (forgeButton != null) forgeButton.interactable = hasNode;
        }
        else
        {
            if (result != null) result.enabled = false;
        }
    }

    public void ExecuteFusion()
    {
        if (currentValidRecipe == null || PlayerInventoryCore.Instance == null) return;

        PlayerInventoryCore.Instance.RemoveEquippedItem(slotA.SlottedEcho);
        PlayerInventoryCore.Instance.RemoveEquippedItem(slotB.SlottedEcho);

        EchoData newEcho = Instantiate(currentValidRecipe.resultEcho);
        newEcho.InitRuntime();
        PlayerInventoryCore.Instance.TryEquip(newEcho);

        slotA.ClearSlot();
        slotB.ClearSlot();

        if (result != null) result.enabled = false;

        PopulateInventory();

        currentValidRecipe = null;
        if (forgeButton != null) forgeButton.interactable = false;
    }
}
