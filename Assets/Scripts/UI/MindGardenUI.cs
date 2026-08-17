using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    [Header("Scrolling View")]
    public ScrollRect scrollRect;
    public Vector2 focusOffset = Vector2.zero;
    public float focusZoomScale = 1f;

    private Dictionary<string, SkillNodeUI> _nodeDict = new Dictionary<string, SkillNodeUI>();
    private Coroutine _focusCoroutine;

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

        // Ensure anchors and scaled UI parents have their final runtime layout
        // before connector endpoints are measured.
        Canvas.ForceUpdateCanvases();
        DrawConnections();

        if (GameManager.Instance != null)
            GameManager.Instance.OnGamePaused += OnGamePaused;

        if (GameInput.Instance != null)
            GameInput.Instance.OnCancelPressed += HandleCancelPressed;

        if (MetaProgressionManager.Instance != null)
        {
            MetaProgressionManager.Instance.OnDataChanged += HandleDataChanged;
            MetaProgressionManager.Instance.OnSkillUnlocked += HandleSkillUnlocked;
        }

        IsOpen = false;
        HideImmediate();
    }

    private void DrawConnections()
    {
        foreach (SkillNodeUI childNode in _allNodes)
        {
            if (childNode == null || childNode.Data == null) continue;

            foreach (MindGardenNodeData prereq in childNode.Data.Prerequisites)
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

        if (MetaProgressionManager.Instance != null)
        {
            MetaProgressionManager.Instance.OnDataChanged -= HandleDataChanged;
            MetaProgressionManager.Instance.OnSkillUnlocked -= HandleSkillUnlocked;
        }
    }

    private void HandleDataChanged()
    {
        RefreshAllNodes();
        UpdateCurrencyLabels();
        OnDataChanged?.Invoke();
    }

    private void HandleSkillUnlocked(MindGardenNodeData skill)
    {
        ShowSkillInfo(skill, SkillNodeUI.NodeState.Unlocked);
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
        if (MetaProgressionManager.Instance != null)
        {
            MetaProgressionManager.Instance.BuyPermanentMaxHP(hpAmount, cost);
        }
    }

    public void ShowSkillInfo(MindGardenNodeData skill, SkillNodeUI.NodeState state)
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
            if (state == SkillNodeUI.NodeState.Unlocked)
            {
                _infoCost.text = "— Already Unlocked —";
            }
            else
            {
                int invested = GameSession.Instance?.currentProfile?.GetInvestedAmount(skill.SkillID) ?? 0;
                int remainingCost = skill.MemoryCost - invested;
                
                if (invested > 0)
                {
                    _infoCost.text = $"Cost: {remainingCost} Astral Shards ({invested}/{skill.MemoryCost} invested)";
                }
                else
                {
                    _infoCost.text = $"Cost: {skill.MemoryCost} Astral Shards";
                }
            }
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

        if (scrollRect != null && EventSystem.current.currentSelectedGameObject != null)
        {
            RectTransform target = EventSystem.current.currentSelectedGameObject.GetComponent<RectTransform>();
            if (target != null)
            {
                if (_focusCoroutine != null) StopCoroutine(_focusCoroutine);
                _focusCoroutine = StartCoroutine(FocusNodeRoutine(target));
            }
        }
    }

    private IEnumerator FocusNodeRoutine(RectTransform target)
    {
        if (scrollRect == null || target == null) yield break;

        RectTransform viewRect = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();
        Vector2 viewportCenter = viewRect.rect.center;
        
        // Calculate the target position relative to the content's pivot
        Vector2 targetPosition = scrollRect.content.InverseTransformPoint(target.position);
        
        float elapsedTime = 0f;
        float duration = 0.25f;
        Vector2 startPos = scrollRect.content.anchoredPosition;
        Vector3 startScale = scrollRect.content.localScale;
        Vector3 endScale = new Vector3(focusZoomScale, focusZoomScale, 1f);

        // Calculate final position accounting for scale
        Vector2 endPos = viewportCenter - (targetPosition * focusZoomScale) + focusOffset;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / duration);
            
            scrollRect.content.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            scrollRect.content.localScale = Vector3.Lerp(startScale, endScale, t);
            
            yield return null;
        }

        scrollRect.content.anchoredPosition = endPos;
        scrollRect.content.localScale = endScale;
    }

    public void ClearSkillInfo() => SetInfoPanelVisible(false);

    private void HandleUnlockRequested(MindGardenNodeData skill)
    {
        if (MetaProgressionManager.Instance != null)
        {
            MetaProgressionManager.Instance.RequestUnlock(skill);
        }
    }

    /// <summary>Called by a SkillNodeUI when it is first-clicked, to deselect all other nodes.</summary>
    public void UnfocusAllNodes()
    {
        foreach (SkillNodeUI node in _allNodes)
        {
            if (node != null)
                node.Unfocus();
        }
    }

    public void RefreshAllNodes()
    {
        if (!ValidateReferences()) return;

        ProfileData profile = GameSession.Instance.currentProfile;
        
        foreach (SkillNodeUI node in _allNodes)
        {
            if (node != null && node.Data != null)
            {
                node.Refresh(profile);
                string status = profile.HasSkill(node.Data.SkillID) ? "UNLOCKED" : "LOCKED";
                Debug.Log($"[MindGardenUI Debug] Skill '{node.Data.SkillName}' ({node.Data.SkillID}) is {status}.");
            }
        }

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

    public void UpdateCurrencyOnly()
    {
        UpdateCurrencyLabels();
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

    private List<string> BuildMissingPrerequisitesList(MindGardenNodeData skill)
    {
        var missing = new List<string>();
        if (!ValidateReferences()) return missing;

        ProfileData profile = GameSession.Instance.currentProfile;
        foreach (MindGardenNodeData prereq in skill.Prerequisites)
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
            Debug.LogError("[MindGardenUI] GameSession or currentProfile is null.");
            return false;
        }

        return true;
    }
}
