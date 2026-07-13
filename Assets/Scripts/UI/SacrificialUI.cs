using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SacrificialUI : MonoBehaviour, IUIPanel
{
    [Header("Inventory Display")]
    [SerializeField] private Transform inventoryGrid;
    [SerializeField] private GameObject draggableElement;

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
        if (inventoryGrid == null || draggableElement == null || PlayerInventoryCore.Instance == null) return;

        foreach (Transform child in inventoryGrid)
        {
            Destroy(child.gameObject);
        }

        IReadOnlyList<ItemBaseData> elements = PlayerInventoryCore.Instance.GetEquippedList(ItemCategory.Echo);
        int maxSlots = PlayerInventoryCore.Instance.UnlockedEchoSlots;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject go = Instantiate(draggableElement, inventoryGrid);
            DraggableElement dragElement = go.GetComponent<DraggableElement>();
            
            if (dragElement != null)
            {
                if (elements != null && i < elements.Count)
                {
                    EchoData echoData = elements[i] as EchoData;
                    dragElement.Setup(echoData);
                }
                else
                {
                    dragElement.Setup(null);
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

        if (slotA.SlottedElement == null || slotB.SlottedElement == null)
        {
            if (result != null) result.enabled = false;
            return;
        }

        foreach (var recipe in recipes)
        {
            if ((recipe.elementA.itemID == slotA.SlottedElement.itemID && recipe.elementB.itemID == slotB.SlottedElement.itemID) ||
                (recipe.elementA.itemID == slotB.SlottedElement.itemID && recipe.elementB.itemID == slotA.SlottedElement.itemID))
            {
                currentValidRecipe = recipe;
                break;
            }
        }

        if (currentValidRecipe != null)
        {
            if (result != null)
            {
                result.sprite = currentValidRecipe.resultElement.itemIcon;
                result.enabled = true;
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

        PlayerInventoryCore.Instance.RemoveEquippedItem(slotA.SlottedElement);
        PlayerInventoryCore.Instance.RemoveEquippedItem(slotB.SlottedElement);

        EchoData newElement = Instantiate(currentValidRecipe.resultElement);
        newElement.InitRuntime();
        PlayerInventoryCore.Instance.TryEquip(newElement);

        slotA.ClearSlot();
        slotB.ClearSlot();

        if (result != null) result.enabled = false;

        PopulateInventory();

        currentValidRecipe = null;
        if (forgeButton != null) forgeButton.interactable = false;
    }
}
