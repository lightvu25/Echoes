using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueConversation conversation;
    [SerializeField] private DialogueSpeaker sourceSpeaker;
    [SerializeField] private UnityEvent onConversationCompleted;

    public void Interact()
    {
        if (DialogueController.Instance == null)
        {
            Debug.LogError($"[{nameof(DialogueInteractable)}] No active DialogueController exists.", this);
            return;
        }

        if (conversation == null)
        {
            Debug.LogWarning($"[{nameof(DialogueInteractable)}] {name} has no conversation assigned.", this);
            return;
        }

        DialogueSpeaker speaker = sourceSpeaker != null ? sourceSpeaker : GetComponentInParent<DialogueSpeaker>();
        if (speaker == null)
        {
            Debug.LogWarning($"[{nameof(DialogueInteractable)}] {name} needs a DialogueSpeaker component.", this);
            return;
        }

        DialogueController.Instance.StartConversation(
            conversation,
            speaker,
            () => onConversationCompleted?.Invoke());
    }

    private void Reset()
    {
        sourceSpeaker = GetComponentInParent<DialogueSpeaker>();
    }
}
