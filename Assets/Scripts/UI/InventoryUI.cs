using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using System;

/// <summary>
/// Main inventory panel controller.
///
/// Responsibilities
/// ----------------
/// • Shows / hides the inventory UI panel with a blur effect.
/// • Enters "Slot Unlock Mode" when <see cref="PlayerInventoryCore.OnSlotUnlockRequired"/> fires.
///   In this mode, Time.timeScale is set to 0 and <see cref="SlotUnlockPanel"/> is shown.
/// • After the player chooses a slot, resumes the game and hides the panel.
///
/// Decoupling note
/// ---------------
/// This class subscribes to PlayerInventoryCore events; it never polls inventory state directly.
/// All state mutations go back through PlayerInventoryCore method calls.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private SlotUnlockPanel slotUnlockPanel;

    [Header("Blur Effect")]
    [SerializeField] private Volume blurVolume;
    [SerializeField] private float blurDuration = 0.3f;

    public bool IsOpen { get; private set; } = false;
    private bool isInUnlockMode = false;

    // ------------------------------------------------------------------ //
    //  Unity lifecycle                                                     //
    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        GameManager.Instance.OnGamePaused         += GameManager_OnGamePaused;
        PlayerInventoryCore.Instance.OnSlotUnlockRequired += HandleSlotUnlockRequired;
        PlayerInventoryCore.Instance.OnInventoryChanged   += HandleInventoryChanged;

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
    }

    private void Update()
    {
        // Allow closing the regular inventory view with Escape, but NOT the unlock panel.
        if (IsOpen && !isInUnlockMode && Input.GetKeyDown(KeyCode.Escape))
            Hide();
    }

    // ------------------------------------------------------------------ //
    //  Event handlers                                                      //
    // ------------------------------------------------------------------ //

    private void GameManager_OnGamePaused(object sender, EventArgs e)
    {
        // If the game is paused externally while we're in unlock mode,
        // stay open — the unlock must be resolved first.
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

    // ------------------------------------------------------------------ //
    //  Public API                                                          //
    // ------------------------------------------------------------------ //

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

    /// <summary>
    /// Called by <see cref="SlotUnlockPanel"/> when the player clicks a category slot.
    /// Unlocks the chosen slot, resumes time, and hides the panel.
    /// </summary>
    /// <param name="chosenCategory">The category the player chose to unlock.</param>
    public void OnSlotChosen(ItemCategory chosenCategory)
    {
        PlayerInventoryCore.Instance.UnlockSlot(chosenCategory);
        Hide();
    }

    // ------------------------------------------------------------------ //
    //  Private helpers                                                     //
    // ------------------------------------------------------------------ //

    private void EnterUnlockMode()
    {
        isInUnlockMode = true;
        Time.timeScale = 0f;
        Show();
        slotUnlockPanel?.Display(
            PlayerInventoryCore.Instance.UnlockedElementSlots,
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