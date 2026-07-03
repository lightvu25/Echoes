using UnityEngine;
using DG.Tweening;

public enum UIAnimationType { FadeOnly, PopUp, SlideUp }

[RequireComponent(typeof(CanvasGroup))]
public class UIPanelAnimator : MonoBehaviour
{
    [SerializeField] private UIAnimationType animationType = UIAnimationType.PopUp;
    [SerializeField] private float animationDuration = 0.3f;

    private CanvasGroup _canvasGroup;
    private CanvasGroup CanvasGroup => _canvasGroup != null ? _canvasGroup : (_canvasGroup = GetComponent<CanvasGroup>());

    private Vector3 _originalScale;
    private Vector3 _originalPosition;

    public bool IsShowing { get; private set; } = false;

    private void Awake()
    {
        _originalPosition = transform.localPosition;
        _originalScale = transform.localScale;
        
        if (_originalScale.sqrMagnitude < 0.01f)
        {
            _originalScale = Vector3.one;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        IsShowing = true;
        
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.interactable = true;

        transform.DOKill();
        CanvasGroup.DOKill();

        if (_originalScale.sqrMagnitude < 0.01f)
        {
            _originalScale = Vector3.one;
        }
        switch (animationType)
        {
            case UIAnimationType.FadeOnly:
                CanvasGroup.DOFade(1f, animationDuration).SetUpdate(true);
                break;

            case UIAnimationType.PopUp:
                transform.localScale = _originalScale * 0.5f;
                transform.DOScale(_originalScale, animationDuration).SetEase(Ease.OutBack).SetUpdate(true);
                CanvasGroup.DOFade(1f, animationDuration).SetUpdate(true);
                break;

            case UIAnimationType.SlideUp:
                var startPos = _originalPosition;
                startPos.y -= 50f;
                transform.localPosition = startPos;
                transform.DOLocalMove(_originalPosition, animationDuration).SetEase(Ease.OutBack).SetUpdate(true);
                CanvasGroup.DOFade(1f, animationDuration).SetUpdate(true);
                break;
        }
    }

    public void Hide()
    {
        if (!IsShowing) return;

        IsShowing = false;
        CanvasGroup.blocksRaycasts = false;
        CanvasGroup.interactable = false;

        transform.DOKill();
        CanvasGroup.DOKill();

        switch (animationType)
        {
            case UIAnimationType.FadeOnly:
                CanvasGroup.DOFade(0f, animationDuration).SetUpdate(true)
                    .OnComplete(() => {
                        gameObject.SetActive(false);
                        CanvasGroup.alpha = 1f;
                    });
                break;

            case UIAnimationType.PopUp:
                transform.DOScale(_originalScale * 0.5f, animationDuration).SetEase(Ease.InBack).SetUpdate(true);
                CanvasGroup.DOFade(0f, animationDuration).SetUpdate(true)
                    .OnComplete(() => {
                        gameObject.SetActive(false);
                        transform.localScale = _originalScale;
                    });
                break;

            case UIAnimationType.SlideUp:
                var targetPos = _originalPosition;
                targetPos.y -= 50f;
                transform.DOLocalMove(targetPos, animationDuration).SetEase(Ease.InBack).SetUpdate(true);
                CanvasGroup.DOFade(0f, animationDuration).SetUpdate(true)
                    .OnComplete(() => {
                        gameObject.SetActive(false);
                        transform.localPosition = _originalPosition;
                    });
                break;
        }
    }

    public void HideImmediate()
    {
        IsShowing = false;
        transform.DOKill();
        CanvasGroup.DOKill();
        CanvasGroup.alpha = 0f;

        transform.localScale = _originalScale;
        transform.localPosition = _originalPosition;

        gameObject.SetActive(false);
    }
}