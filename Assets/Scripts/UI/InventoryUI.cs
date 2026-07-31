using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using System;

public class InventoryUI : MonoBehaviour, IUIPanel
{
    [Header("Panels")]
    [SerializeField] private GameObject uiRoot;

    [Header("Blur Effect")]
    [SerializeField] private Volume blurVolume;
    [SerializeField] private float blurDuration = 0.3f;

    public static event Action<bool> OnInventoryToggled;

    public bool IsOpen { get; private set; } = false;
    public bool IsInUnlockMode { get; private set; } = false;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGamePaused += GameManager_OnGamePaused;
        }

        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnSlotUnlockRequired += HandleSlotUnlockRequired;
            PlayerInventoryCore.Instance.OnInventoryChanged   += HandleInventoryChanged;
        }

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnCancelPressed += HandleCancelPressed;
            GameInput.Instance.OnInventoryPressed += HandleInventoryToggle;
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGamePaused -= GameManager_OnGamePaused;

        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnSlotUnlockRequired -= HandleSlotUnlockRequired;
            PlayerInventoryCore.Instance.OnInventoryChanged   -= HandleInventoryChanged;
        }

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnCancelPressed -= HandleCancelPressed;
            GameInput.Instance.OnInventoryPressed -= HandleInventoryToggle;
        }
    }

    private void HandleInventoryToggle()
    {
        if (IsOpen) 
        {
            UIManager.Instance.ClosePanelIfOpen(UIPanelType.Inventory);
        }
        else if (!IsOpen)
        {
            UIManager.Instance.OpenPanel(UIPanelType.Inventory);
        }
    }

    private void HandleCancelPressed()
    {
        if (IsOpen) 
        {
            UIManager.Instance.ClosePanelIfOpen(UIPanelType.Inventory);
        }
    }

    private void GameManager_OnGamePaused(object sender, EventArgs e)
    {
        if (IsOpen) 
        {
            UIManager.Instance.ClosePanelIfOpen(UIPanelType.Inventory);
        }
    }

    private void HandleSlotUnlockRequired()
    {
        // Gaining max HP auto-opens the inventory so they can spend their point
        if (!IsOpen) Show();
        else CheckUnlockMode();
    }

    private void HandleInventoryChanged()
    {
        // Refresh slot visuals if the panel is currently open.
        if (IsOpen) 
        {
            RefreshSlots();
            CheckUnlockMode();
        }
    }


    public void Show()
    {
        IsOpen = true;
        if (uiRoot != null) uiRoot.SetActive(true);
        AnimateBlur(1f);
        
        CheckUnlockMode();
        
        OnInventoryToggled?.Invoke(true);
    }

    public void Hide()
    {
        bool wasInUnlockMode = IsInUnlockMode;
        IsInUnlockMode = false;
        IsOpen = false;
        if (uiRoot != null) uiRoot.SetActive(false);
        AnimateBlur(0f);
        OnInventoryToggled?.Invoke(false);
        
        // Only force time back to 1 if we were the ones who froze it via Unlock Mode
        // Otherwise let UIManager or GameManager handle time scale.
        if (wasInUnlockMode)
        {
            if (TimeManager.Instance != null) TimeManager.Instance.ResumeTime("InventoryUnlock");
            else Time.timeScale = 1f;
        }
    }

    public static InventoryUI Instance { get; private set; } // Added singleton for easy access from slots

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void TryConsumeUnlockPoint(ItemCategory category, int slotIndex)
    {
        if (IsInUnlockMode && PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.AvailableUnlockPoints > 0)
        {
            PlayerInventoryCore.Instance.UnlockSlot(category, slotIndex);
            
            if (PlayerInventoryCore.Instance.AvailableUnlockPoints <= 0)
            {
                Hide();
            }
            else
            {
                CheckUnlockMode();
            }
        }
    }

    private void CheckUnlockMode()
    {
        if (PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.AvailableUnlockPoints > 0)
        {
            IsInUnlockMode = true;
            if (TimeManager.Instance != null) TimeManager.Instance.PauseTime("InventoryUnlock");
            else Time.timeScale = 0f;

            var allSlots = GetComponentsInChildren<InventorySlot>(true);
            foreach (var slot in allSlots)
            {
                if (!PlayerInventoryCore.Instance.IsSlotUnlocked(slot.Category, slot.SlotIndex))
                    slot.SetGlowing(true);
                else
                    slot.SetGlowing(false);
            }
        }
        else
        {
            IsInUnlockMode = false;
            var allSlots = GetComponentsInChildren<InventorySlot>(true);
            foreach (var slot in allSlots)
            {
                slot.SetGlowing(false);
            }
        }
    }

    private void RefreshSlots()
    {
        // Subclasses or inspector-wired slot UIs will respond to OnInventoryChanged directly.
        // Nothing additional needed here yet.
    }

    private void AnimateBlur(float target)
    {
        if (blurVolume == null) return;
        blurVolume.DOKill();
        DOTween.To(() => blurVolume.weight, x => blurVolume.weight = x, target, blurDuration)
               .SetUpdate(true); // SetUpdate(true) so it runs while timeScale == 0
    }
}