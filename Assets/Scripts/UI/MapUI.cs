using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class MapUI : MonoBehaviour, IUIPanel
{
    public bool IsOpen { get; private set; }

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        // Start completely invisible, but keep the GameObject ACTIVE 
        // so we don't unregister from GameInput!
        HideImmediate();
    }

    private void OnEnable()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnCancelPressed += HandleCancelPressed;
            GameInput.Instance.OnMapTogglePressed += HandleMapToggle;
        }
    }

    private void OnDisable()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnCancelPressed -= HandleCancelPressed;
            GameInput.Instance.OnMapTogglePressed -= HandleMapToggle;
        }
    }

    private void HandleMapToggle()
    {
        if (IsOpen)
        {
            Hide();
            if (UIManager.Instance != null && UIManager.Instance.IsAnyPanelOpen)
            {
                UIManager.Instance.CloseCurrentPanel();
            }
        }
        else
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel(UIPanelType.Map);
            }
        }
    }

    private void HandleCancelPressed()
    {
        if (IsOpen && UIManager.Instance != null)
        {
            UIManager.Instance.CloseCurrentPanel();
        }
    }

    public void Show()
    {
        IsOpen = true;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        
        transform.DOKill();
        _canvasGroup.DOKill();

        transform.localScale = Vector3.one * 0.5f;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        _canvasGroup.DOFade(1f, 0.3f).SetUpdate(true);
    }

    public void Hide()
    {
        IsOpen = false;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        
        transform.DOKill();
        _canvasGroup.DOKill();

        transform.DOScale(Vector3.one * 0.5f, 0.3f).SetEase(Ease.InBack).SetUpdate(true);
        _canvasGroup.DOFade(0f, 0.3f).SetUpdate(true);
    }

    private void HideImmediate()
    {
        IsOpen = false;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _canvasGroup.alpha = 0f;
    }
}
