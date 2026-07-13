using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIZoomable : MonoBehaviour, IScrollHandler
{
    [Tooltip("The content RectTransform that will be scaled (e.g., MapContent).")]
    [SerializeField] private RectTransform content;

    [Tooltip("How fast it zooms in/out.")]
    [SerializeField] private float zoomSpeed = 0.5f;

    [Tooltip("Minimum allowed scale (e.g., 0.5 for 50% size).")]
    [SerializeField] private float minZoom = 0.2f;

    [Tooltip("Maximum allowed scale (e.g., 3.0 for 300% size).")]
    [SerializeField] private float maxZoom = 5f;

    private float _targetScale = 1f;

    private void Start()
    {
        if (content != null)
        {
            _targetScale = content.localScale.x;
        }

        // Disable vertical/horizontal panning from the scroll wheel on the parent ScrollRect
        UnityEngine.UI.ScrollRect scrollRect = GetComponent<UnityEngine.UI.ScrollRect>();
        if (scrollRect == null && content != null)
        {
            scrollRect = content.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        }
        
        if (scrollRect != null)
        {
            scrollRect.scrollSensitivity = 0f;
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (content == null) return;

        float scrollDelta = eventData.scrollDelta.y;
        if (Mathf.Abs(scrollDelta) < 0.01f) return;

        float oldTargetScale = _targetScale;
        _targetScale += scrollDelta * zoomSpeed;
        _targetScale = Mathf.Clamp(_targetScale, minZoom, maxZoom);

        if (Mathf.Approximately(oldTargetScale, _targetScale)) return;

        Vector2 localMousePos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(content, eventData.position, eventData.pressEventCamera, out localMousePos))
        {
            // Calculate shift based on the difference between the NEW target scale and the CURRENT physical scale
            Vector2 shift = localMousePos * (_targetScale - content.localScale.x);
            Vector2 newPos = content.anchoredPosition - shift;

            content.DOKill();
            content.DOScale(_targetScale, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
            content.DOAnchorPos(newPos, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        }
        else
        {
            content.DOKill();
            content.DOScale(_targetScale, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        }
    }
}
