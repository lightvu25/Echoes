using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueSpeakerRole
{
    Source,
    Player
}

[Serializable]
public class DialogueLine
{
    [SerializeField] private DialogueSpeakerRole speaker = DialogueSpeakerRole.Source;
    [SerializeField, TextArea(2, 8)] private string text;

    public DialogueSpeakerRole Speaker => speaker;
    public string Text => text ?? string.Empty;

    public DialogueLine(DialogueSpeakerRole speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}

[CreateAssetMenu(fileName = "Dialogue Conversation", menuName = "Echoes/Dialogue/Conversation")]
public class DialogueConversation : ScriptableObject
{
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    public IReadOnlyList<DialogueLine> Lines => lines;
    public int LineCount => lines != null ? lines.Count : 0;

    public bool TryGetLine(int index, out DialogueLine line)
    {
        line = null;
        if (lines == null || index < 0 || index >= lines.Count) return false;

        line = lines[index];
        return line != null && !string.IsNullOrWhiteSpace(line.Text);
    }
}
