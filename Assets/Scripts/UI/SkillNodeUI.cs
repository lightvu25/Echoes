using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkillNodeUI : MonoBehaviour,
    ISelectHandler,      IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISubmitHandler
{
    [Header("Data")]
    [SerializeField] private ConstellationData _data;

    [Header("Node Visuals")]
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _holdProgressImage;
    [SerializeField] private Image _borderImage;

    [Header("State Colours")]
    [SerializeField] private Color _colorLocked    = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color _colorAvailable = new Color(1f,    0.85f, 0.2f,  1f);
    [SerializeField] private Color _colorUnlocked  = new Color(0.3f,  0.85f, 0.45f, 1f);

    [Header("Hold Settings")]
    [Tooltip("Seconds the player must hold to trigger the purchase.")]
    [Min(0.1f)]
    [SerializeField] private float _holdDuration = 1.2f;

    public RectTransform RectTransform { get; private set; }

    [Header("State Colours")]
    public Color colorLocked    = new Color(0.25f, 0.25f, 0.25f, 1f);
    public Color colorAvailable = new Color(1f,    0.85f, 0.2f,  1f);
    public Color colorUnlocked  = new Color(0.3f,  0.85f, 0.45f, 1f);

    private Button    _button;
    private NodeState _currentState = NodeState.Locked;
    private bool      _isHolding;
    private Coroutine _holdCoroutine;

    public enum NodeState { Locked, Available, Unlocked }

    public event Action<ConstellationData> OnUnlockRequested;

    public ConstellationData Data => _data;

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

    private void Awake()
    {
        _button = GetComponent<Button>();
        RectTransform = GetComponent<RectTransform>();

        if (_holdProgressImage != null)
        {
            _holdProgressImage.type       = Image.Type.Filled;
            _holdProgressImage.fillMethod = Image.FillMethod.Radial360;
            _holdProgressImage.fillAmount = 0f;
        }
    }

    private void OnDisable()
    {
        CancelHold();
    }

    public void OnSelect(BaseEventData eventData) => NotifyManager();

    public void OnDeselect(BaseEventData eventData) => UIManager.Instance?.GetPanel<MindGardenUI>(UIPanelType.MindGarden)?.ClearSkillInfo();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        EventSystem.current.SetSelectedGameObject(gameObject);
        BeginHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        CancelHold();
    }

    public void OnSubmit(BaseEventData eventData) => BeginHold();

    private void BeginHold()
    {
        if (_currentState != NodeState.Available || _isHolding) return;

        _isHolding     = true;
        _holdCoroutine = StartCoroutine(HoldRoutine());
    }

    private void CancelHold()
    {
        if (!_isHolding) return;

        _isHolding = false;

        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }

        if (_holdProgressImage != null)
            _holdProgressImage.fillAmount = 0f;
    }

    private IEnumerator HoldRoutine()
    {
        float elapsed = 0f;

        while (elapsed < _holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (_holdProgressImage != null)
                _holdProgressImage.fillAmount = Mathf.Clamp01(elapsed / _holdDuration);

            yield return null;
        }

        if (_holdProgressImage != null)
            _holdProgressImage.fillAmount = 1f;

        _isHolding     = false;
        _holdCoroutine = null;

        OnUnlockRequested?.Invoke(_data);
    }

    private void ApplyState(NodeState state)
    {
        _currentState = state;

        bool isInteractable  = state == NodeState.Available;
        _button.interactable = isInteractable;

        Color target = state switch
        {
            NodeState.Unlocked  => _colorUnlocked,
            NodeState.Available => _colorAvailable,
            _                   => _colorLocked,
        };

        if (_iconImage       != null) _iconImage.color       = target;
        if (_backgroundImage != null) _backgroundImage.color = target;
        if (_borderImage     != null) _borderImage.color     = target;

        if (_holdProgressImage != null)
        {
            _holdProgressImage.fillAmount = 0f;
            _holdProgressImage.gameObject.SetActive(isInteractable);
        }

        if (_iconImage != null && _data?.Icon != null)
            _iconImage.sprite = _data.Icon;
    }

    private void NotifyManager()
    {
        if (EventSystem.current.currentSelectedGameObject == gameObject)
        {
            var ui = UIManager.Instance?.GetPanel<MindGardenUI>(UIPanelType.MindGarden);
            ui?.ShowSkillInfo(_data, _currentState);
        }
    }
}
