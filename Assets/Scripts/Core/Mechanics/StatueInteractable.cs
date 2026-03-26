using UnityEngine;

/// <summary>
/// A Statue placed in the game world. When the player interacts with it,
/// the Statue opens the StatueUIManager panel for banking, withdrawing, or
/// purchasing permanent upgrades.
/// Inherits from InteractableObject for trigger detection and input handling.
/// </summary>
public class StatueInteractable : InteractableObject
{
    [Header("Statue Settings")]
    [Tooltip("If true, time is slowed when the statue menu is open.")]
    [SerializeField] private bool slowTimeOnOpen = true;
    [SerializeField] private float slowTimeScale = 0.1f;

    protected override void OnInteract()
    {
        if (StatueUIManager.Instance == null)
        {
            Debug.LogWarning("[StatueInteractable] StatueUIManager.Instance is null. " +
                             "Ensure a StatueUIManager exists in the scene.");
            return;
        }

        // Optionally slow time to give a "menu opened" feel
        if (slowTimeOnOpen)
        {
            Time.timeScale = slowTimeScale;
        }

        SetPromptVisible(false);
        StatueUIManager.Instance.OpenUI();
    }

    protected override void OnPlayerExit(Collider2D player)
    {
        // If the player somehow walks out while the menu is open, close it cleanly
        if (StatueUIManager.Instance != null && StatueUIManager.Instance.IsOpen)
        {
            StatueUIManager.Instance.CloseUI();
        }
    }
}
