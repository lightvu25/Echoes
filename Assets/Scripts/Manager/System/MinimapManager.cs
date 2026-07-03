using UnityEngine;
using System.Collections;

public class MinimapManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform minimapRect;

    [Header("Expanded Settings")]
    [SerializeField] private Vector2 expandedSize = new Vector2(800, 800);
    [SerializeField] private Vector2 expandedPosition = Vector2.zero;

    [Header("Animation")]
    [SerializeField] private float transitionSpeed = 15f;

    private Vector2 originalSize;
    private Vector2 originalPosition;
    private bool isExpanded = false;
    private Coroutine transitionCoroutine;

    private void Start()
    {
        if (minimapRect == null) minimapRect = GetComponent<RectTransform>();

        originalSize = minimapRect.sizeDelta;
        originalPosition = minimapRect.anchoredPosition;

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnMenuButtonPressed += HandleMenuPressed;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnMenuButtonPressed -= HandleMenuPressed;
        }
    }

    private void HandleMapToggle()
    {
        isExpanded = !isExpanded;
        AnimateMap();
    }

    private void HandleMenuPressed(object sender, System.EventArgs e)
    {
        if (isExpanded)
        {
            isExpanded = false;
            AnimateMap();
        }
    }

    private void AnimateMap()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        Vector2 targetSize = isExpanded ? expandedSize : originalSize;
        Vector2 targetPosition = isExpanded ? expandedPosition : originalPosition;

        transitionCoroutine = StartCoroutine(SmoothTransition(targetSize, targetPosition));
    }

    private IEnumerator SmoothTransition(Vector2 targetSize, Vector2 targetPos)
    {
        while (Vector2.Distance(minimapRect.sizeDelta, targetSize) > 0.5f)
        {
            minimapRect.sizeDelta = Vector2.Lerp(minimapRect.sizeDelta, targetSize, Time.unscaledDeltaTime * transitionSpeed);
            minimapRect.anchoredPosition = Vector2.Lerp(minimapRect.anchoredPosition, targetPos, Time.unscaledDeltaTime * transitionSpeed);
            yield return null;
        }
        minimapRect.sizeDelta = targetSize;
        minimapRect.anchoredPosition = targetPos;
    }
}