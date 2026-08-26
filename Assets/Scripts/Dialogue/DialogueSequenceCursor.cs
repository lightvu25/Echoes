using System.Collections.Generic;

/// <summary>
/// Owns deterministic conversation progress separately from Unity presentation.
/// Invalid or empty authored lines are skipped safely.
/// </summary>
public sealed class DialogueSequenceCursor
{
    private readonly IReadOnlyList<DialogueLine> lines;
    private int index = -1;

    public DialogueSequenceCursor(IReadOnlyList<DialogueLine> lines)
    {
        this.lines = lines;
    }

    public int Index => index;
    public DialogueLine Current { get; private set; }

    public bool MoveNext()
    {
        if (lines == null)
        {
            Current = null;
            return false;
        }

        while (++index < lines.Count)
        {
            DialogueLine candidate = lines[index];
            if (candidate == null || string.IsNullOrWhiteSpace(candidate.Text)) continue;

            Current = candidate;
            return true;
        }

        Current = null;
        return false;
    }
}
