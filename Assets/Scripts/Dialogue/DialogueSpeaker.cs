using UnityEngine;

[DisallowMultipleComponent]
public class DialogueSpeaker : MonoBehaviour
{
    [SerializeField] private string displayName = "Speaker";
    [Tooltip("Child transform where the popup tail should point. Create a child named DialogueAnchor above the head.")]
    [SerializeField] private Transform dialogueAnchor;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public Transform DialogueAnchor => dialogueAnchor != null ? dialogueAnchor : transform;

    private void Reset()
    {
        Transform existingAnchor = transform.Find("DialogueAnchor");
        if (existingAnchor != null) dialogueAnchor = existingAnchor;
    }

    private void OnValidate()
    {
        if (dialogueAnchor != null || transform == null) return;

        Transform existingAnchor = transform.Find("DialogueAnchor");
        if (existingAnchor != null) dialogueAnchor = existingAnchor;
    }
}
