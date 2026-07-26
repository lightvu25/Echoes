using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using System;

public class InventoryUI : MonoBehaviour, IUIPanel
{
    [Header("Panels")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private SlotUnlockPanel slotUnlockPanel;

    [Header("Blur Effect")]
    [SerializeField] private Volume blurVolume;
    [SerializeField] private float blurDuration = 0.3f;

    public bool IsOpen { get; private set; } = false;
    private bool isInUnlockMode = false;

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
        if (IsOpen && !isInUnlockMode) 
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
        if (IsOpen && !isInUnlockMode) 
        {
            UIManager.Instance.ClosePanelIfOpen(UIPanelType.Inventory);
        }
    }

    private void GameManager_OnGamePaused(object sender, EventArgs e)
    {
        if (IsOpen && !isInUnlockMode) 
        {
            UIManager.Instance.ClosePanelIfOpen(UIPanelType.Inventory);
        }
    }

    private void HandleSlotUnlockRequired()
    {
        EnterUnlockMode();
    }

    private void HandleInventoryChanged()
    {
        // Refresh slot visuals if the panel is currently open.
        if (IsOpen) RefreshSlots();
    }


    public void Show()
    {
        IsOpen = true;
        if (uiRoot != null) uiRoot.SetActive(true);
        AnimateBlur(1f);
    }

    public void Hide()
    {
        bool wasInUnlockMode = isInUnlockMode;
        isInUnlockMode = false;
        IsOpen = false;
        if (uiRoot != null) uiRoot.SetActive(false);
        AnimateBlur(0f);
        
        // Only force time back to 1 if we were the ones who froze it via Unlock Mode
        // Otherwise let UIManager or GameManager handle time scale.
        if (wasInUnlockMode)
        {
            if (TimeManager.Instance != null) TimeManager.Instance.ResumeTime("InventoryUnlock");
            else Time.timeScale = 1f;
        }
    }

    public void OnSlotChosen(ItemCategory chosenCategory)
    {
        PlayerInventoryCore.Instance.UnlockSlot(chosenCategory);
        Hide();
    }

    private void EnterUnlockMode()
    {
        isInUnlockMode = true;
        if (TimeManager.Instance != null) TimeManager.Instance.PauseTime("InventoryUnlock");
        else Time.timeScale = 0f;
        Show();
        slotUnlockPanel?.Display(
            PlayerInventoryCore.Instance.UnlockedEchoSlots,
            PlayerInventoryCore.Instance.UnlockedRelicSlots,
            PlayerInventoryCore.Instance.UnlockedEquipmentSlots);
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