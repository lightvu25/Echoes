using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialogueBubbleView : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform bubbleFrame;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Optional References")]
    [SerializeField] private RectTransform tail;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private LayoutElement frameLayout;
    [SerializeField] private LayoutElement dialogueLayout;

    [Header("Responsive Size (1920 x 1080 reference)")]
    [SerializeField, Min(1f)] private float minWidth = 240f;
    [SerializeField, Min(1f)] private float maxWidth = 600f;
    [SerializeField, Min(1)] private int maxLinesPerPage = 4;
    [SerializeField] private Vector2 padding = new Vector2(24f, 16f);
    [SerializeField, Min(0f)] private float speakerNameGap = 8f;

    [Header("Follow")]
    [SerializeField, Min(0f)] private float anchorGap = 28f;
    [SerializeField, Min(0f)] private float screenMargin = 24f;
    [SerializeField, Min(0f)] private float tailEdgePadding = 24f;
    [SerializeField] private bool flipBelowNearScreenTop = true;
    [SerializeField] private bool closeWhenSpeakerLeavesScreen = true;

    [Header("Presentation")]
    [SerializeField, Min(1f)] private float charactersPerSecond = 45f;
    [SerializeField, Min(0f)] private float openDuration = 0.16f;

    private RectTransform rootRect;
    private RectTransform overlayRect;
    private Canvas targetCanvas;
    private Camera worldCamera;
    private Transform anchor;
    private Coroutine typewriterRoutine;
    private int currentPage = 1;
    private int pageCount = 1;
    private int pageFirstCharacter;
    private int pageLastCharacter;
    private bool isTyping;
    private bool isPrepared;

    public bool IsTyping => isTyping;
    public bool HasMorePages => currentPage < pageCount;

    private void Awake()
    {
        rootRect = transform as RectTransform;
    }

    private void OnEnable()
    {
        ResetVisualState();
    }

    private void OnDisable()
    {
        StopTypewriter();
        transform.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
        anchor = null;
        overlayRect = null;
        targetCanvas = null;
        worldCamera = null;
        isPrepared = false;
    }

    public bool Prepare(RectTransform overlay, Canvas canvas, Camera camera)
    {
        if (rootRect == null) rootRect = transform as RectTransform;
        if (rootRect == null || overlay == null || canvasGroup == null || bubbleFrame == null || dialogueText == null)
        {
            Debug.LogError($"[{nameof(DialogueBubbleView)}] Required UI references are missing on {name}.", this);
            return false;
        }

        overlayRect = overlay;
        targetCanvas = canvas;
        worldCamera = camera;
        rootRect.SetParent(overlayRect, false);
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.localRotation = Quaternion.identity;
        rootRect.localScale = Vector3.one;
        isPrepared = true;
        return true;
    }

    public void ShowLine(DialogueSpeaker speaker, string text)
    {
        if (!isPrepared || speaker == null) return;

        anchor = speaker.DialogueAnchor;
        StopTypewriter();

        bool showName = speakerNameText != null && !string.IsNullOrWhiteSpace(speaker.DisplayName);
        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(showName);
            speakerNameText.text = showName ? speaker.DisplayName : string.Empty;
        }

        dialogueText.text = text ?? string.Empty;
        dialogueText.enableWordWrapping = true;
        dialogueText.overflowMode = TextOverflowModes.Overflow;
        dialogueText.maxVisibleCharacters = int.MaxValue;
        dialogueText.pageToDisplay = 1;

        ApplyResponsiveSize(showName);

        dialogueText.overflowMode = TextOverflowModes.Page;
        dialogueText.ForceMeshUpdate();
        pageCount = Mathf.Max(1, dialogueText.textInfo.pageCount);
        currentPage = 1;
        dialogueText.pageToDisplay = currentPage;
        BeginCurrentPage();
        PlayOpenAnimation();
        UpdatePosition();
    }

    public void CompleteTyping()
    {
        if (!isTyping) return;

        StopTypewriter();
        dialogueText.maxVisibleCharacters = pageLastCharacter + 1;
        SetContinueIndicator(true);
    }

    public bool ShowNextPage()
    {
        if (isTyping || !HasMorePages) return false;

        currentPage++;
        dialogueText.pageToDisplay = currentPage;
        BeginCurrentPage();
        return true;
    }

    public bool UpdatePosition()
    {
        if (!isPrepared || anchor == null || overlayRect == null || rootRect == null) return false;

        Camera camera = worldCamera != null ? worldCamera : Camera.main;
        if (camera == null) return false;

        Vector3 screenPoint3 = camera.WorldToScreenPoint(anchor.position);
        if (screenPoint3.z < 0f) return false;

        if (closeWhenSpeakerLeavesScreen &&
            (screenPoint3.x < 0f || screenPoint3.x > Screen.width || screenPoint3.y < 0f || screenPoint3.y > Screen.height))
        {
            return false;
        }

        Camera uiCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                new Vector2(screenPoint3.x, screenPoint3.y),
                uiCamera,
                out Vector2 anchorLocal))
        {
            return false;
        }

        Vector2 size = bubbleFrame.rect.size;
        if (size.x <= 0f || size.y <= 0f) size = rootRect.rect.size;

        Rect bounds = overlayRect.rect;
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        float minX = bounds.xMin + screenMargin + halfWidth;
        float maxX = bounds.xMax - screenMargin - halfWidth;
        float minY = bounds.yMin + screenMargin + halfHeight;
        float maxY = bounds.yMax - screenMargin - halfHeight;

        bool placeBelow = flipBelowNearScreenTop && anchorLocal.y + anchorGap + size.y > bounds.yMax - screenMargin;
        float desiredY = placeBelow
            ? anchorLocal.y - anchorGap - halfHeight
            : anchorLocal.y + anchorGap + halfHeight;

        Vector2 finalPosition = new Vector2(
            ClampEvenWhenOversized(anchorLocal.x, minX, maxX),
            ClampEvenWhenOversized(desiredY, minY, maxY));
        finalPosition.x = Mathf.Round(finalPosition.x);
        finalPosition.y = Mathf.Round(finalPosition.y);
        rootRect.anchoredPosition = finalPosition;

        if (tail != null)
        {
            float tailX = Mathf.Clamp(
                anchorLocal.x - finalPosition.x,
                -halfWidth + tailEdgePadding,
                halfWidth - tailEdgePadding);
            tail.anchoredPosition = new Vector2(Mathf.Round(tailX), placeBelow ? halfHeight : -halfHeight);
            tail.localRotation = Quaternion.Euler(0f, 0f, placeBelow ? 180f : 0f);
        }

        return true;
    }

    public void ResetForPool()
    {
        StopTypewriter();
        transform.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = int.MaxValue;
            dialogueText.pageToDisplay = 1;
        }
        if (speakerNameText != null) speakerNameText.text = string.Empty;
        SetContinueIndicator(false);
        anchor = null;
        isPrepared = false;
    }

    private void ApplyResponsiveSize(bool showName)
    {
        float maximumBodyWidth = Mathf.Max(1f, maxWidth - padding.x * 2f);
        Vector2 bodyPreferred = dialogueText.GetPreferredValues(dialogueText.text, maximumBodyWidth, 0f);
        Vector2 namePreferred = showName
            ? speakerNameText.GetPreferredValues(speakerNameText.text, maximumBodyWidth, 0f)
            : Vector2.zero;
        float singleLineHeight = Mathf.Max(1f, dialogueText.GetPreferredValues("Ag", maximumBodyWidth, 0f).y);

        DialogueBubbleSize size = DialogueBubbleSizing.Calculate(
            bodyPreferred,
            namePreferred,
            singleLineHeight,
            maxLinesPerPage,
            minWidth,
            maxWidth,
            padding,
            speakerNameGap);

        if (frameLayout != null)
        {
            frameLayout.preferredWidth = size.FrameWidth;
            frameLayout.preferredHeight = size.FrameHeight;
        }
        if (dialogueLayout != null)
        {
            dialogueLayout.preferredWidth = size.BodyWidth;
            dialogueLayout.preferredHeight = size.BodyHeight;
        }

        bubbleFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.FrameWidth);
        bubbleFrame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.FrameHeight);
        dialogueText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.BodyWidth);
        dialogueText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.BodyHeight);
        rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.FrameWidth);
        rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.FrameHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleFrame);
        Canvas.ForceUpdateCanvases();
    }

    private void BeginCurrentPage()
    {
        dialogueText.ForceMeshUpdate();
        int availablePages = Mathf.Max(1, dialogueText.textInfo.pageCount);
        currentPage = Mathf.Clamp(currentPage, 1, availablePages);
        pageCount = availablePages;

        TMP_PageInfo pageInfo = dialogueText.textInfo.pageInfo[currentPage - 1];
        pageFirstCharacter = Mathf.Max(0, pageInfo.firstCharacterIndex);
        pageLastCharacter = Mathf.Max(pageFirstCharacter, pageInfo.lastCharacterIndex);
        dialogueText.maxVisibleCharacters = pageFirstCharacter;
        SetContinueIndicator(false);

        isTyping = pageLastCharacter >= pageFirstCharacter;
        if (isTyping) typewriterRoutine = StartCoroutine(TypeCurrentPage());
        else SetContinueIndicator(true);
    }

    private IEnumerator TypeCurrentPage()
    {
        float visibleCharacters = pageFirstCharacter;
        while (visibleCharacters < pageLastCharacter + 1)
        {
            visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
            dialogueText.maxVisibleCharacters = Mathf.Min(pageLastCharacter + 1, Mathf.FloorToInt(visibleCharacters));
            yield return null;
        }

        dialogueText.maxVisibleCharacters = pageLastCharacter + 1;
        typewriterRoutine = null;
        isTyping = false;
        SetContinueIndicator(true);
    }

    private void StopTypewriter()
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }
        isTyping = false;
    }

    private void PlayOpenAnimation()
    {
        transform.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();

        transform.localScale = Vector3.one * 0.92f;
        canvasGroup.alpha = 0f;
        transform.DOScale(Vector3.one, openDuration).SetEase(Ease.OutBack).SetUpdate(true);
        canvasGroup.DOFade(1f, openDuration).SetUpdate(true);
    }

    private void ResetVisualState()
    {
        StopTypewriter();
        transform.DOKill();
        transform.localScale = Vector3.one;
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        SetContinueIndicator(false);
    }

    private void SetContinueIndicator(bool visible)
    {
        if (continueIndicator != null) continueIndicator.SetActive(visible);
    }

    private static float ClampEvenWhenOversized(float value, float min, float max)
    {
        return min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;
    }
}
