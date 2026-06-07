using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatueUI : MonoBehaviour
{
    public static StatueUI Instance { get; private set; }

    public event Action OnDataChanged;

    public bool IsOpen { get; private set; }

    public int RunMems    => PlayerStats.Instance != null ? PlayerStats.Instance.MemoryFragments : 0;
    public int BankedMems => GameSession.Instance  != null ? GameSession.Instance.currentProfile.bankedMems : 0;

    [Header("Skill Nodes")]
    [SerializeField] private List<SkillNodeUI> _allNodes = new List<SkillNodeUI>();

    [Header("Info Panel")]
    [SerializeField] private GameObject      _infoPanelRoot;
    [SerializeField] private Image           _infoIcon;
    [SerializeField] private TextMeshProUGUI _infoName;
    [SerializeField] private TextMeshProUGUI _infoDescription;
    [SerializeField] private TextMeshProUGUI _infoCost;
    [SerializeField] private TextMeshProUGUI _infoPrerequisites;
    [SerializeField] private TextMeshProUGUI _infoStatus;
    [SerializeField] private GameObject      _infoDefaultMessage;

    [Header("Currency Display")]
    [SerializeField] private TextMeshProUGUI _bankedMemsLabel;
    [SerializeField] private TextMeshProUGUI _runMemsLabel;

    [Header("Panel")]
    [SerializeField] private UIPanelAnimator _panelAnimator;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        foreach (SkillNodeUI node in _allNodes)
        {
            if (node != null)
                node.OnUnlockRequested += HandleUnlockRequested;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnGamePaused += OnGamePaused;

        IsOpen = false;
        HideImmediate();
    }

    private void OnDestroy()
    {
        foreach (SkillNodeUI node in _allNodes)
        {
            if (node != null)
                node.OnUnlockRequested -= HandleUnlockRequested;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnGamePaused -= OnGamePaused;
    }

    private void OnGamePaused(object sender, EventArgs e) => Hide();

    public void Show()
    {
        IsOpen = true;
        RefreshAllNodes();
        UpdateCurrencyLabels();

        if (_panelAnimator != null) _panelAnimator.Show();
        else gameObject.SetActive(true);

        ClearSkillInfo();
        OnDataChanged?.Invoke();
    }

    public void Hide()
    {
        IsOpen = false;
        if (_panelAnimator != null) _panelAnimator.Hide();
        else gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        if (_panelAnimator != null) _panelAnimator.HideImmediate();
        else gameObject.SetActive(false);
    }

    public void DepositMems(int amount)
    {
        if (!ValidateReferences() || amount <= 0) return;
        if (!PlayerStats.Instance.SpendMemoryFragments(amount)) return;

        GameSession.Instance.currentProfile.bankedMems += amount;
        SaveManager.saveProfile(GameSession.Instance.currentProfile);
        UpdateCurrencyLabels();
        OnDataChanged?.Invoke();
    }

    public void WithdrawMems(int amount)
    {
        if (!ValidateReferences() || amount <= 0) return;
        if (GameSession.Instance.currentProfile.bankedMems < amount) return;

        GameSession.Instance.currentProfile.bankedMems -= amount;
        SaveManager.saveProfile(GameSession.Instance.currentProfile);
        PlayerStats.Instance.AddMemoryFragments(amount);
        UpdateCurrencyLabels();
        OnDataChanged?.Invoke();
    }

    public void ShowSkillInfo(StatueSkillData skill, SkillNodeUI.NodeState state)
    {
        if (skill == null) return;

        SetInfoPanelVisible(true);

        if (_infoIcon != null)
        {
            _infoIcon.sprite  = skill.Icon;
            _infoIcon.enabled = skill.Icon != null;
        }

        if (_infoName        != null) _infoName.text        = skill.SkillName;
        if (_infoDescription != null) _infoDescription.text = skill.Description;

        if (_infoCost != null)
        {
            _infoCost.text = state == SkillNodeUI.NodeState.Unlocked
                ? "— Already Unlocked —"
                : $"Cost: {skill.MemoryCost} Memory Fragments";
        }

        if (_infoPrerequisites != null)
        {
            if (state == SkillNodeUI.NodeState.Locked)
            {
                List<string> missing = BuildMissingPrerequisitesList(skill);
                _infoPrerequisites.text = missing.Count > 0
                    ? "Requires:\n• " + string.Join("\n• ", missing)
                    : string.Empty;
                _infoPrerequisites.gameObject.SetActive(missing.Count > 0);
            }
            else
            {
                _infoPrerequisites.gameObject.SetActive(false);
            }
        }

        if (_infoStatus != null)
        {
            _infoStatus.text = state switch
            {
                SkillNodeUI.NodeState.Unlocked  => "✓ UNLOCKED",
                SkillNodeUI.NodeState.Available => "AVAILABLE — Hold to Unlock",
                _                               => "LOCKED",
            };
        }
    }

    public void ClearSkillInfo() => SetInfoPanelVisible(false);

    private void HandleUnlockRequested(StatueSkillData skill)
    {
        if (skill == null || !ValidateReferences()) return;

        ProfileData profile = GameSession.Instance.currentProfile;

        if (profile.HasSkill(skill.SkillID))
        {
            Debug.LogWarning($"[StatueUIManager] Skill '{skill.SkillID}' is already unlocked.");
            return;
        }

        if (!skill.ArePrerequisitesMet(profile.unlockedSkillIDs))
        {
            Debug.LogWarning($"[StatueUIManager] Prerequisites for '{skill.SkillID}' not met.");
            return;
        }

        if (profile.bankedMems < skill.MemoryCost)
        {
            Debug.LogWarning($"[StatueUIManager] Not enough Memory Fragments " +
                             $"(have {profile.bankedMems}, need {skill.MemoryCost}).");
            return;
        }

        profile.bankedMems -= skill.MemoryCost;
        profile.unlockedSkillIDs.Add(skill.SkillID);
        SaveManager.saveProfile(profile);

        RefreshAllNodes();
        UpdateCurrencyLabels();
        ShowSkillInfo(skill, SkillNodeUI.NodeState.Unlocked);
        OnDataChanged?.Invoke();

        Debug.Log($"[StatueUIManager] '{skill.SkillName}' unlocked. Remaining banked mems: {profile.bankedMems}.");
    }

    public void RefreshAllNodes()
    {
        if (!ValidateReferences()) return;

        ProfileData profile = GameSession.Instance.currentProfile;
        foreach (SkillNodeUI node in _allNodes)
            node?.Refresh(profile);
    }

    private void UpdateCurrencyLabels()
    {
        if (!ValidateReferences()) return;

        if (_bankedMemsLabel != null)
            _bankedMemsLabel.text = $"{GameSession.Instance.currentProfile.bankedMems} Banked";

        if (_runMemsLabel != null)
            _runMemsLabel.text = $"{RunMems} Run Frags";
    }

    private void SetInfoPanelVisible(bool visible)
    {
        if (_infoPanelRoot      != null) _infoPanelRoot.SetActive(visible);
        if (_infoDefaultMessage != null) _infoDefaultMessage.SetActive(!visible);
    }

    private List<string> BuildMissingPrerequisitesList(StatueSkillData skill)
    {
        var missing = new List<string>();
        if (!ValidateReferences()) return missing;

        ProfileData profile = GameSession.Instance.currentProfile;
        foreach (StatueSkillData prereq in skill.Prerequisites)
        {
            if (prereq != null && !profile.HasSkill(prereq.SkillID))
                missing.Add(prereq.SkillName);
        }
        return missing;
    }

    private bool ValidateReferences()
    {
        if (GameSession.Instance == null || GameSession.Instance.currentProfile == null)
        {
            Debug.LogError("[StatueUIManager] GameSession or currentProfile is null.");
            return false;
        }

        return true;
    }
}
