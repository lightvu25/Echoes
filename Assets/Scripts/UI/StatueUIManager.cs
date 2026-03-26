using System;
using UnityEngine;

/// <summary>
/// Singleton UI Logic Hub for the Statue system.
/// Manages banking/withdrawing runMemoryFragments and purchasing
/// permanent upgrades stored on ProfileData.
/// 
/// Wire up UI Canvas buttons in the Editor to call:
///   StatueUIManager.Instance.DepositMems(amount)
///   StatueUIManager.Instance.WithdrawMems(amount)
///   StatueUIManager.Instance.UpgradeMaxHP(cost)
///   StatueUIManager.Instance.CloseUI()
/// </summary>
public class StatueUIManager : MonoBehaviour
{
    public static StatueUIManager Instance { get; private set; }

    // ===== Events =====
    /// <summary>Fires whenever any stat or currency changes so UI can refresh itself.</summary>
    public event Action OnDataChanged;

    // ===== Config =====
    [Header("UI Root")]
    [Tooltip("Root GameObject of the Statue UI Canvas panel.")]
    [SerializeField] private GameObject statueUIPanel;

    [Header("Upgrade Costs")]
    [Tooltip("Cost in banked mems to increase Max HP by 1 slot.")]
    [SerializeField] private int maxHPUpgradeCost = 50;

    // ===== State =====
    public bool IsOpen { get; private set; } = false;

    // ===== Accessors (for UI display) =====
    public int RunMems       => PlayerStats.Instance != null ? PlayerStats.Instance.MemoryFragments : 0;
    public int BankedMems    => GameSession.Instance != null ? GameSession.Instance.currentProfile.bankedMems : 0;
    public int BonusMaxHP    => GameSession.Instance != null ? GameSession.Instance.currentProfile.bonusStartingMaxHP : 0;
    public int MaxHPCost     => maxHPUpgradeCost;

    // ===== Lifecycle =====
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        CloseUI(); // Start hidden
    }

    // ===== UI Controls =====

    /// <summary>
    /// Opens the Statue menu panel and pauses time if not already.
    /// </summary>
    public void OpenUI()
    {
        IsOpen = true;
        if (statueUIPanel != null)
        {
            statueUIPanel.SetActive(true);
        }
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// Closes the Statue menu and restores normal time scale.
    /// </summary>
    public void CloseUI()
    {
        IsOpen = false;
        Time.timeScale = 1f;
        if (statueUIPanel != null)
        {
            statueUIPanel.SetActive(false);
        }
    }

    // ===== Banking Operations =====

    /// <summary>
    /// Transfers <paramref name="amount"/> memory fragments from the current run
    /// into the persistent profile bank (safe from death loss).
    /// </summary>
    /// <param name="amount">Number of fragments to bank.</param>
    public void DepositMems(int amount)
    {
        if (!ValidateReferences()) return;
        if (amount <= 0) return;

        // SpendMemoryFragments already validates against current balance
        bool spent = PlayerStats.Instance.SpendMemoryFragments(amount);
        if (!spent)
        {
            Debug.Log("[StatueUIManager] Not enough runMemoryFragments to deposit.");
            return;
        }

        GameSession.Instance.currentProfile.bankedMems += amount;
        SaveManager.saveProfile(GameSession.Instance.currentProfile);

        Debug.Log($"[StatueUIManager] Deposited {amount} mems. Banked total: {BankedMems}");
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// Withdraws <paramref name="amount"/> memory fragments from the profile bank
    /// back into the active run (they can be lost on death again).
    /// </summary>
    /// <param name="amount">Number of fragments to withdraw.</param>
    public void WithdrawMems(int amount)
    {
        if (!ValidateReferences()) return;
        if (amount <= 0) return;

        if (GameSession.Instance.currentProfile.bankedMems < amount)
        {
            Debug.Log("[StatueUIManager] Not enough bankedMems to withdraw.");
            return;
        }

        GameSession.Instance.currentProfile.bankedMems -= amount;
        SaveManager.saveProfile(GameSession.Instance.currentProfile);

        PlayerStats.Instance.AddMemoryFragments(amount); // Also calls SaveCurrentRun internally

        Debug.Log($"[StatueUIManager] Withdrew {amount} mems. Banked remaining: {BankedMems}");
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// Spends banked memory fragments to permanently increase the player's starting Max HP.
    /// Costs <see cref="maxHPUpgradeCost"/> banked mems per purchase.
    /// The field on ProfileData is <c>bonusStartingMaxHP</c>.
    /// </summary>
    /// <param name="cost">Override cost, or use 0 to use the default <see cref="maxHPUpgradeCost"/>.</param>
    public void UpgradeMaxHP(int cost = 0)
    {
        if (!ValidateReferences()) return;

        int actualCost = cost > 0 ? cost : maxHPUpgradeCost;

        if (GameSession.Instance.currentProfile.bankedMems < actualCost)
        {
            Debug.Log($"[StatueUIManager] Not enough bankedMems. Need {actualCost}, have {BankedMems}.");
            return;
        }

        GameSession.Instance.currentProfile.bankedMems -= actualCost;
        GameSession.Instance.currentProfile.bonusStartingMaxHP += 1;
        SaveManager.saveProfile(GameSession.Instance.currentProfile);

        Debug.Log($"[StatueUIManager] Max HP upgraded. bonusStartingMaxHP = {BonusMaxHP}. Banked remaining: {BankedMems}");
        OnDataChanged?.Invoke();
    }

    // ===== Internal Helpers =====

    private bool ValidateReferences()
    {
        if (PlayerStats.Instance == null)
        {
            Debug.LogError("[StatueUIManager] PlayerStats.Instance is null.");
            return false;
        }
        if (GameSession.Instance == null || GameSession.Instance.currentProfile == null)
        {
            Debug.LogError("[StatueUIManager] GameSession or currentProfile is null.");
            return false;
        }
        return true;
    }
}
