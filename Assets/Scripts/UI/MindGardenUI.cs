using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MindGardenUI : MonoBehaviour, IUIPanel
{
    public event Action OnDataChanged;

    public bool IsOpen { get; private set; }

    public int RunShards => PlayerStats.Instance != null ? PlayerStats.Instance.CurrentAstralShards : 0;

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
    [SerializeField] private TextMeshProUGUI _runShardsLabel;

    [Header("Panel")]
    [SerializeField] private UIPanelAnimator _panelAnimator;

    [Header("Line Settings")]
    public GameObject linePrefab; 
    public Transform starMapContent;

    private Dictionary<string, SkillNodeUI> _nodeDict = new Dictionary<string, SkillNodeUI>();
    private class NodeConnection
    {
        public string childID;
        public Image lineImage;
    }

    private List<NodeConnection> _connections = new List<NodeConnection>();

    private void Start()
    {
        foreach (SkillNodeUI node in _allNodes)
        {
            if (node != null && node.Data != null)
            {
                _nodeDict[node.Data.SkillID] = node;
                node.OnUnlockRequested += HandleUnlockRequested;
            }
        }

        DrawConnections();

        if (GameManager.Instance != null)
            GameManager.Instance.OnGamePaused += OnGamePaused;

        if (GameInput.Instance != null)
            GameInput.Instance.OnCancelPressed += HandleCancelPressed;

        IsOpen = false;
        HideImmediate();
    }

    private void DrawConnections()
    {
        foreach (SkillNodeUI childNode in _allNodes)
        {
            if (childNode == null || childNode.Data == null) continue;

            foreach (ConstellationData prereq in childNode.Data.Prerequisites)
            {
                if (prereq == null) continue;

                if (_nodeDict.TryGetValue(prereq.SkillID, out SkillNodeUI parentNode))
                {
                    GameObject lineObj = UILineDrawer.DrawLine(linePrefab, starMapContent, parentNode.RectTransform, childNode.RectTransform);
                    
                    if (lineObj.TryGetComponent(out Image lineImg))
                    {
                        _connections.Add(new NodeConnection { 
                            childID = childNode.Data.SkillID, 
                            lineImage = lineImg 
                        });
                    }
                }
            }
        }
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

        if (GameInput.Instance != null)
            GameInput.Instance.OnCancelPressed -= HandleCancelPressed;
    }

    private void OnGamePaused(object sender, EventArgs e)
    {
        if (IsOpen && UIManager.Instance != null)
            UIManager.Instance.CloseCurrentPanel();
    }

    private void HandleCancelPressed()
    {
        if (IsOpen && UIManager.Instance != null)
            UIManager.Instance.CloseCurrentPanel();
    }

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

    public void BuyPermanentMaxHP(int hpAmount, int cost)
    {
        if (!ValidateReferences() || cost <= 0) return;
        if (PlayerStats.Instance == null || PlayerStats.Instance.CurrentAstralShards < cost) return;

        if (PlayerStats.Instance.SpendAstralShards(cost))
        {
            GameSession.Instance.currentProfile.bonusStartingMaxHP += hpAmount;
            SaveManager.saveProfile(GameSession.Instance.currentProfile);
            UpdateCurrencyLabels();
            OnDataChanged?.Invoke();
        }
    }

    public void ShowSkillInfo(ConstellationData skill, SkillNodeUI.NodeState state)
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
                : $"Cost: {skill.MemoryCost} Astral Shards";
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

    private void HandleUnlockRequested(ConstellationData skill)
    {
        if (skill == null || !ValidateReferences()) return;

        ProfileData profile = GameSession.Instance.currentProfile;

        if (profile.HasSkill(skill.SkillID))
        {
            Debug.LogWarning($"[ConstellationUIManager] Skill '{skill.SkillID}' is already unlocked.");
            return;
        }

        if (!skill.ArePrerequisitesMet(profile.unlockedSkillIDs))
        {
            Debug.LogWarning($"[ConstellationUIManager] Prerequisites for '{skill.SkillID}' not met.");
            return;
        }

        if (PlayerStats.Instance == null || PlayerStats.Instance.CurrentAstralShards < skill.MemoryCost)
        {
            Debug.LogWarning($"[MindGardenUI] Not enough Astral Shards (have {RunShards}, need {skill.MemoryCost}).");
            return;
        }

        PlayerStats.Instance.SpendAstralShards(skill.MemoryCost);
        profile.unlockedSkillIDs.Add(skill.SkillID);
        SaveManager.saveProfile(profile);

        RefreshAllNodes();
        UpdateCurrencyLabels();
        ShowSkillInfo(skill, SkillNodeUI.NodeState.Unlocked);
        OnDataChanged?.Invoke();

        Debug.Log($"[MindGardenUI] '{skill.SkillName}' unlocked. Remaining run shards: {RunShards}.");
    }

    public void RefreshAllNodes()
    {
        if (!ValidateReferences()) return;

        ProfileData profile = GameSession.Instance.currentProfile;
        
        foreach (SkillNodeUI node in _allNodes)
            node?.Refresh(profile);

        UpdateLineColors(profile);
    }

    private void UpdateLineColors(ProfileData profile)
    {
        if (_allNodes.Count == 0 || _allNodes[0] == null) return;
        Color unlockedColor = _allNodes[0].colorUnlocked;
        Color lockedColor = _allNodes[0].colorLocked;

        foreach (NodeConnection conn in _connections)
        {
            if (conn.lineImage != null)
            {
                if (profile.HasSkill(conn.childID))
                {
                    conn.lineImage.color = unlockedColor;
                }
                else
                {
                    conn.lineImage.color = lockedColor;
                }
            }
        }
    }

    private void UpdateCurrencyLabels()
    {
        if (!ValidateReferences()) return;

        if (_runShardsLabel != null)
            _runShardsLabel.text = $"{RunShards}";
    }

    private void SetInfoPanelVisible(bool visible)
    {
        if (_infoPanelRoot      != null) _infoPanelRoot.SetActive(visible);
        if (_infoDefaultMessage != null) _infoDefaultMessage.SetActive(!visible);
    }

    private List<string> BuildMissingPrerequisitesList(ConstellationData skill)
    {
        var missing = new List<string>();
        if (!ValidateReferences()) return missing;

        ProfileData profile = GameSession.Instance.currentProfile;
        foreach (ConstellationData prereq in skill.Prerequisites)
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
            Debug.LogError("[ConstellationUIManager] GameSession or currentProfile is null.");
            return false;
        }

        return true;
    }
}
