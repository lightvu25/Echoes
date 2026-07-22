using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkillNodeUI : MonoBehaviour,
    ISelectHandler,      IDeselectHandler,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    [SerializeField] private MindGardenNodeData _data;

    [Header("Node Visuals")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _holdProgressImage;
    [SerializeField] private Image _borderImage;

    [Header("Hold Settings")]
    [Tooltip("Total seconds the player must hold to fully invest all shards.")]
    [Min(0.1f)]
    [SerializeField] private float _holdDuration = 1.2f;

    public RectTransform RectTransform { get; private set; }

    [Header("State Colours")]
    public Color colorLocked    = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color colorAvailable = new Color(1f,    0.85f, 0.2f,  1f);
    public Color colorUnlocked  = new Color(0.3f,  0.85f, 0.45f, 1f);

    private Button    _button;
    private NodeState _currentState = NodeState.Locked;

    // --- Self-managed focus state (NOT relying on EventSystem, which ScrollRect breaks) ---
    private bool      _isFocused;     // true after first click on this node
    private bool      _mouseIsDown;   // true while LMB is physically held over this node
    private bool      _isHolding;     // true while the drain coroutine is active
    private Coroutine _holdCoroutine;

    public enum NodeState { Locked, Available, Unlocked }

    public event Action<MindGardenNodeData> OnUnlockRequested;

    public MindGardenNodeData Data => _data;

    // -----------------------------------------------------------------
    //  Unity lifecycle
    // -----------------------------------------------------------------

    private void Awake()
    {
        _button       = GetComponent<Button>();
        RectTransform = GetComponent<RectTransform>();

        if (_holdProgressImage != null)
        {
            _holdProgressImage.type       = Image.Type.Filled;
            _holdProgressImage.fillMethod = Image.FillMethod.Radial360;
            _holdProgressImage.fillAmount = 0f;
        }
    }

    private void Update()
    {
        // Only drain shards when this node is focused AND available
        if (!_isFocused || _currentState != NodeState.Available) return;

        // Accept LMB hold OR Space key hold
        bool holdInput = _mouseIsDown || Input.GetKey(KeyCode.Space);

        if (holdInput && !_isHolding)
        {
            BeginHold();
        }
        else if (!holdInput && _isHolding)
        {
            StopHold();
        }
    }

    private void OnDisable()
    {
        Unfocus();
    }

    // -----------------------------------------------------------------
    //  EventSystem handlers (for visual highlight only)
    // -----------------------------------------------------------------

    public void OnSelect(BaseEventData eventData)
    {
        ShowInfoPanel();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Do NOT clear _isFocused here — ScrollRect fires this spuriously.
        // Focus is managed manually via Focus() / Unfocus().
    }

    // -----------------------------------------------------------------
    //  Pointer handlers
    // -----------------------------------------------------------------

    public void OnPointerClick(PointerEventData eventData)
    {
        // Not used, logic moved to OnPointerDown
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (!_isFocused)
        {
            // First click: tell all siblings to unfocus, then focus this node
            var ui = UIManager.Instance?.GetPanel<MindGardenUI>(UIPanelType.MindGarden);
            ui?.UnfocusAllNodes();
            Focus();
            
            // We intentionally DO NOT set _mouseIsDown = true here.
            // This guarantees the first click will NEVER consume shards, even if held.
        }
        else
        {
            // Second click: already focused, so we allow the hold-to-consume
            _mouseIsDown = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _mouseIsDown = false;
    }

    // Swallowing drag events prevents the parent ScrollRect from stealing the pointer
    // and cancelling our hold action prematurely.
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData) { }

    // -----------------------------------------------------------------
    //  Focus management (self-managed, immune to ScrollRect)
    // -----------------------------------------------------------------

    /// <summary>Select this node: show info panel and enable hold-to-drain.</summary>
    public void Focus()
    {
        _isFocused = true;
        EventSystem.current.SetSelectedGameObject(gameObject); // visual highlight only
        ShowInfoPanel();
    }

    /// <summary>Deselect this node. Called by MindGardenUI when another node is clicked.</summary>
    public void Unfocus()
    {
        _isFocused   = false;
        _mouseIsDown = false;
        StopHold();
    }

    // -----------------------------------------------------------------
    //  Hold / drain logic
    // -----------------------------------------------------------------

    private void BeginHold()
    {
        if (_currentState != NodeState.Available || _isHolding) return;

        Debug.Log($"[SkillNodeUI] BeginHold on {_data?.SkillID}");
        _isHolding     = true;
        _holdCoroutine = StartCoroutine(HoldRoutine());
    }

    private void StopHold()
    {
        if (!_isHolding) return;

        Debug.Log($"[SkillNodeUI] StopHold on {_data?.SkillID}");
        _isHolding = false; // while-loop in coroutine exits naturally, which saves progress
    }

    private IEnumerator HoldRoutine()
    {
        ProfileData profile = GameSession.Instance?.currentProfile;
        if (profile == null || PlayerStats.Instance == null) { _isHolding = false; yield break; }

        int totalCost     = _data.MemoryCost;
        int invested      = profile.GetInvestedAmount(_data.SkillID);
        int remainingCost = totalCost - invested;

        if (remainingCost <= 0) { _isHolding = false; yield break; }

        // drainRate: shards per second based on full cost / hold duration
        float drainRate   = (float)totalCost / _holdDuration;
        float accumulator = 0f;

        while (_isHolding && remainingCost > 0)
        {
            if (PlayerStats.Instance.CurrentAstralShards <= 0)
                break; // ran out — stop draining, save partial progress

            accumulator += drainRate * Time.unscaledDeltaTime;

            if (accumulator >= 1f)
            {
                int toDrain = Mathf.FloorToInt(accumulator);
                toDrain     = Mathf.Min(toDrain, remainingCost, PlayerStats.Instance.CurrentAstralShards);

                if (toDrain > 0 && PlayerStats.Instance.SpendAstralShards(toDrain))
                {
                    remainingCost -= toDrain;
                    accumulator   -= toDrain;

                    profile.SetInvestedAmount(_data.SkillID, totalCost - remainingCost);

                    if (_holdProgressImage != null)
                        _holdProgressImage.fillAmount = (float)(totalCost - remainingCost) / totalCost;

                    UIManager.Instance?.GetPanel<MindGardenUI>(UIPanelType.MindGarden)?.UpdateCurrencyOnly();
                }
            }

            yield return null;
        }

        // --- Coroutine finished ---
        if (remainingCost <= 0)
        {
            Debug.Log($"[SkillNodeUI] Fully paid {_data.SkillID}! Unlocking.");
            if (_holdProgressImage != null) _holdProgressImage.fillAmount = 1f;
            OnUnlockRequested?.Invoke(_data);
        }
        else
        {
            // Partial investment — persist and refresh the info panel cost text
            SaveManager.saveProfile(profile);
            UIManager.Instance?.GetPanel<MindGardenUI>(UIPanelType.MindGarden)
                               ?.ShowSkillInfo(_data, _currentState);
        }

        _isHolding     = false;
        _holdCoroutine = null;
    }

    // -----------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------

    public void Refresh(ProfileData profile)
    {
        if (_data == null || profile == null) return;

        if (profile.HasSkill(_data.SkillID))
            ApplyState(NodeState.Unlocked);
        else if (_data.ArePrerequisitesMet(profile.unlockedSkillIDs))
            ApplyState(NodeState.Available);
        else
            ApplyState(NodeState.Locked);
    }

    private void ShowInfoPanel()
    {
        UIManager.Instance?.GetPanel<MindGardenUI>(UIPanelType.MindGarden)
                           ?.ShowSkillInfo(_data, _currentState);
    }

    private void ApplyState(NodeState state)
    {
        _currentState = state;

        _button.interactable = true; // always interactable so EventSystem can select it

        Color target = state switch
        {
            NodeState.Unlocked  => colorUnlocked,
            NodeState.Available => colorAvailable,
            _                   => colorLocked,
        };

        if (_iconImage       != null) _iconImage.color       = target;
        if (_backgroundImage != null) _backgroundImage.color = target;
        if (_borderImage     != null) _borderImage.color     = target;

        bool isInteractable = state == NodeState.Available;

        if (_holdProgressImage != null)
        {
            if (state == NodeState.Unlocked)
            {
                _holdProgressImage.fillAmount = 1f;
            }
            else if (state == NodeState.Available)
            {
                int inv = GameSession.Instance?.currentProfile?.GetInvestedAmount(_data.SkillID) ?? 0;
                _holdProgressImage.fillAmount = (float)inv / Mathf.Max(1, _data.MemoryCost);
            }
            else
            {
                _holdProgressImage.fillAmount = 0f;
            }
            _holdProgressImage.gameObject.SetActive(isInteractable || state == NodeState.Unlocked);
        }

        if (_iconImage != null && _data?.Icon != null)
            _iconImage.sprite = _data.Icon;
    }
}
