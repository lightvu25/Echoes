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
            Hide();
        }
        else if (!IsOpen)
        {
            UIManager.Instance.OpenPanel(UIPanelType.Inventory);
        }
    }

    private void HandleCancelPressed()
    {
        if (IsOpen && !isInUnlockMode) Hide();
    }

    private void GameManager_OnGamePaused(object sender, EventArgs e)
    {
        if (!isInUnlockMode) Hide();
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
        isInUnlockMode = false;
        IsOpen = false;
        if (uiRoot != null) uiRoot.SetActive(false);
        AnimateBlur(0f);
        Time.timeScale = 1f;
    }

    public void OnSlotChosen(ItemCategory chosenCategory)
    {
        PlayerInventoryCore.Instance.UnlockSlot(chosenCategory);
        Hide();
    }

    private void EnterUnlockMode()
    {
        isInUnlockMode = true;
        Time.timeScale = 0f;
        Show();
        slotUnlockPanel?.Display(
            PlayerInventoryCore.Instance.UnlockedEchoSlots,
            PlayerInventoryCore.Instance.UnlockedRelicSlots,
            PlayerInventoryCore.Instance.UnlockedItemSlots);
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