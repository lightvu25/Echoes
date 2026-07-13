using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIDragHandler : MonoBehaviour, IDragHandler
{
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition += eventData.delta;
        }
    }
}
