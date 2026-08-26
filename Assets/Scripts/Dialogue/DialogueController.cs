using System;
using UnityEngine;

[DisallowMultipleComponent]
public class DialogueController : MonoBehaviour, IUIPanel
{
    private const string FallbackPauseRequester = "DialogueController";

    public static DialogueController Instance { get; private set; }

    [Header("Scene UI")]
    [Tooltip("Full-screen, non-blocking RectTransform under the main Canvas.")]
    [SerializeField] private RectTransform dialogueOverlay;
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject dialogueBubblePrefab;

    [Header("Speakers")]
    [SerializeField] private DialogueSpeaker playerSpeaker;

    [Header("Camera")]
    [Tooltip("Camera that renders the world speakers. Leave empty to use Camera.main.")]
    [SerializeField] private Camera worldCamera;

    [Header("Behavior")]
    [SerializeField] private bool allowCancel = true;

    private DialogueConversation conversation;
    private DialogueSequenceCursor cursor;
    private DialogueSpeaker sourceSpeaker;
    private DialogueBubbleView activeBubble;
    private GameObject activeBubbleObject;
    private Action completionCallback;
    private bool isDialogueActive;
    private bool usesUIManagerPanel;
    private bool fallbackPauseHeld;
    private bool inputSubscribed;
    private bool warnedMissingPlayerSpeaker;

    public bool IsDialogueActive => isDialogueActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (dialogueOverlay == null) dialogueOverlay = transform as RectTransform;
        if (overlayCanvasGroup == null && dialogueOverlay != null)
            overlayCanvasGroup = dialogueOverlay.GetComponent<CanvasGroup>();
        if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();

        SetOverlayVisible(false);
    }

    private void Start()
    {
        TrySubscribeInput();
    }

    private void Update()
    {
        if (!inputSubscribed) TrySubscribeInput();
    }

    private void LateUpdate()
    {
        if (!isDialogueActive || activeBubble == null) return;

        if (!activeBubble.UpdatePosition())
        {
            CloseConversation(false);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeInput();
        ReleaseFallbackPause();
        ReleaseBubble();
        if (Instance == this) Instance = null;
    }

    public bool StartConversation(
        DialogueConversation newConversation,
        DialogueSpeaker newSourceSpeaker,
        Action onCompleted = null)
    {
        if (isDialogueActive) return false;
        if (newConversation == null || newSourceSpeaker == null)
        {
            Debug.LogWarning($"[{nameof(DialogueController)}] A conversation and source speaker are required.", this);
            return false;
        }
        if (dialogueOverlay == null || dialogueBubblePrefab == null)
        {
            Debug.LogError($"[{nameof(DialogueController)}] Assign Dialogue Overlay and Dialogue Bubble Prefab in the Inspector.", this);
            return false;
        }

        DialogueSequenceCursor newCursor = new DialogueSequenceCursor(newConversation.Lines);
        if (!newCursor.MoveNext())
        {
            Debug.LogWarning($"[{nameof(DialogueController)}] {newConversation.name} has no playable dialogue lines.", newConversation);
            return false;
        }

        conversation = newConversation;
        cursor = newCursor;
        sourceSpeaker = newSourceSpeaker;
        completionCallback = onCompleted;
        warnedMissingPlayerSpeaker = false;
        isDialogueActive = true;

        UIManager uiManager = UIManager.Instance;
        usesUIManagerPanel = uiManager != null &&
                             uiManager.GetPanel<DialogueController>(UIPanelType.Dialogue) == this;

        if (usesUIManagerPanel)
        {
            uiManager.OpenPanel(UIPanelType.Dialogue);
            if (!uiManager.IsTimeFrozenByPanel) AcquireFallbackPause();
        }
        else
        {
            Debug.LogWarning(
                $"[{nameof(DialogueController)}] Dialogue is not registered in UIManager. " +
                "The controller will still run and pause using its fallback.",
                this);
            Show();
            AcquireFallbackPause();
        }

        if (activeBubble == null)
        {
            CloseConversation(false);
            return false;
        }

        return true;
    }

    public void Advance()
    {
        if (!isDialogueActive || activeBubble == null) return;

        if (activeBubble.IsTyping)
        {
            activeBubble.CompleteTyping();
            return;
        }

        if (activeBubble.ShowNextPage()) return;

        if (cursor != null && cursor.MoveNext())
        {
            PresentCurrentLine();
            return;
        }

        CloseConversation(true);
    }

    public void Cancel()
    {
        if (!isDialogueActive || !allowCancel) return;
        CloseConversation(false);
    }

    public void Show()
    {
        SetOverlayVisible(true);
        if (!isDialogueActive) return;

        if (!EnsureBubble()) return;
        PresentCurrentLine();
    }

    public void Hide()
    {
        EndLocalState(false);
    }

    private bool EnsureBubble()
    {
        if (activeBubble != null) return true;

        activeBubbleObject = ObjectPoolManager.SpawnObject(
            dialogueBubblePrefab,
            Vector3.zero,
            Quaternion.identity,
            ObjectPoolManager.PoolType.UI);
        activeBubble = activeBubbleObject != null
            ? activeBubbleObject.GetComponentInChildren<DialogueBubbleView>(true)
            : null;

        if (activeBubble == null)
        {
            Debug.LogError(
                $"[{nameof(DialogueController)}] Dialogue Bubble Prefab needs a {nameof(DialogueBubbleView)} component.",
                dialogueBubblePrefab);
            if (activeBubbleObject != null) ObjectPoolManager.ReturnObjectToPool(activeBubbleObject);
            activeBubbleObject = null;
            return false;
        }

        Camera camera = worldCamera != null ? worldCamera : Camera.main;
        if (!activeBubble.Prepare(dialogueOverlay, targetCanvas, camera))
        {
            ReleaseBubble();
            return false;
        }

        return true;
    }

    private void PresentCurrentLine()
    {
        if (activeBubble == null || cursor == null || cursor.Current == null) return;

        DialogueSpeaker speaker = ResolveSpeaker(cursor.Current.Speaker);
        if (speaker == null)
        {
            CloseConversation(false);
            return;
        }

        activeBubble.ShowLine(speaker, cursor.Current.Text);
    }

    private DialogueSpeaker ResolveSpeaker(DialogueSpeakerRole role)
    {
        if (role == DialogueSpeakerRole.Source) return sourceSpeaker;

        if (playerSpeaker == null && PlayerInteract.Instance != null)
        {
            playerSpeaker = PlayerInteract.Instance.GetComponentInParent<DialogueSpeaker>();
            if (playerSpeaker == null)
                playerSpeaker = PlayerInteract.Instance.GetComponentInChildren<DialogueSpeaker>();
        }

        if (playerSpeaker != null) return playerSpeaker;

        if (!warnedMissingPlayerSpeaker)
        {
            warnedMissingPlayerSpeaker = true;
            Debug.LogWarning(
                $"[{nameof(DialogueController)}] A Player line was authored, but Player Speaker is not assigned. " +
                "The source speaker anchor will be used instead.",
                this);
        }
        return sourceSpeaker;
    }

    private void CloseConversation(bool completed)
    {
        if (!isDialogueActive) return;

        Action callback = completed ? completionCallback : null;
        bool closeRegisteredPanel = usesUIManagerPanel &&
                                    UIManager.Instance != null &&
                                    UIManager.Instance.CurrentActivePanel == UIPanelType.Dialogue;

        EndLocalState(false);

        if (closeRegisteredPanel) UIManager.Instance.ClosePanelIfOpen(UIPanelType.Dialogue);
        callback?.Invoke();
    }

    private void EndLocalState(bool keepOverlayVisible)
    {
        isDialogueActive = false;
        conversation = null;
        cursor = null;
        sourceSpeaker = null;
        completionCallback = null;
        usesUIManagerPanel = false;
        warnedMissingPlayerSpeaker = false;
        ReleaseBubble();
        ReleaseFallbackPause();
        SetOverlayVisible(keepOverlayVisible);
    }

    private void ReleaseBubble()
    {
        if (activeBubble != null) activeBubble.ResetForPool();
        if (activeBubbleObject != null) ObjectPoolManager.ReturnObjectToPool(activeBubbleObject);
        activeBubble = null;
        activeBubbleObject = null;
    }

    private void SetOverlayVisible(bool visible)
    {
        if (overlayCanvasGroup == null) return;

        overlayCanvasGroup.alpha = visible ? 1f : 0f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;
    }

    private void AcquireFallbackPause()
    {
        if (fallbackPauseHeld) return;

        fallbackPauseHeld = true;
        if (TimeManager.Instance != null) TimeManager.Instance.PauseTime(FallbackPauseRequester);
        else Time.timeScale = 0f;
    }

    private void ReleaseFallbackPause()
    {
        if (!fallbackPauseHeld) return;

        fallbackPauseHeld = false;
        if (TimeManager.Instance != null) TimeManager.Instance.ResumeTime(FallbackPauseRequester);
        else Time.timeScale = 1f;
    }

    private void TrySubscribeInput()
    {
        if (inputSubscribed || GameInput.Instance == null) return;

        GameInput.Instance.OnCancelPressed += HandleCancelPressed;
        inputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!inputSubscribed) return;

        if (GameInput.Instance != null)
            GameInput.Instance.OnCancelPressed -= HandleCancelPressed;
        inputSubscribed = false;
    }

    private void HandleCancelPressed()
    {
        Cancel();
    }
}
