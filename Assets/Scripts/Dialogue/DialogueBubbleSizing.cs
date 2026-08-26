using UnityEngine;

public readonly struct DialogueBubbleSize
{
    public DialogueBubbleSize(float frameWidth, float frameHeight, float bodyWidth, float bodyHeight)
    {
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        BodyWidth = bodyWidth;
        BodyHeight = bodyHeight;
    }

    public float FrameWidth { get; }
    public float FrameHeight { get; }
    public float BodyWidth { get; }
    public float BodyHeight { get; }
}

public static class DialogueBubbleSizing
{
    public static DialogueBubbleSize Calculate(
        Vector2 bodyPreferred,
        Vector2 namePreferred,
        float singleLineHeight,
        int maxLines,
        float minFrameWidth,
        float maxFrameWidth,
        Vector2 padding,
        float nameGap)
    {
        float safeMinWidth = Mathf.Max(1f, minFrameWidth);
        float safeMaxWidth = Mathf.Max(safeMinWidth, maxFrameWidth);
        float bodyMaxWidth = Mathf.Max(1f, safeMaxWidth - padding.x * 2f);
        float bodyMinWidth = Mathf.Max(1f, safeMinWidth - padding.x * 2f);
        float bodyWidth = Mathf.Clamp(Mathf.Max(bodyPreferred.x, namePreferred.x), bodyMinWidth, bodyMaxWidth);

        float maxBodyHeight = Mathf.Max(1f, singleLineHeight) * Mathf.Max(1, maxLines);
        float bodyHeight = Mathf.Clamp(bodyPreferred.y, Mathf.Max(1f, singleLineHeight), maxBodyHeight);
        float shownNameHeight = namePreferred.y > 0f ? namePreferred.y + Mathf.Max(0f, nameGap) : 0f;
        float frameWidth = Mathf.Clamp(bodyWidth + padding.x * 2f, safeMinWidth, safeMaxWidth);
        float frameHeight = bodyHeight + shownNameHeight + padding.y * 2f;

        return new DialogueBubbleSize(frameWidth, frameHeight, bodyWidth, bodyHeight);
    }
}
