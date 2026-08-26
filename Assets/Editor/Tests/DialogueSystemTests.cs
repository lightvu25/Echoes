#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class DialogueSystemTests
{
    [Test]
    public void SequenceCursor_SkipsEmptyLinesAndPreservesSpeakerOrder()
    {
        var lines = new List<DialogueLine>
        {
            new DialogueLine(DialogueSpeakerRole.Source, ""),
            null,
            new DialogueLine(DialogueSpeakerRole.Source, "Hello."),
            new DialogueLine(DialogueSpeakerRole.Player, "Who are you?")
        };
        var cursor = new DialogueSequenceCursor(lines);

        Assert.IsTrue(cursor.MoveNext());
        Assert.AreEqual(2, cursor.Index);
        Assert.AreEqual(DialogueSpeakerRole.Source, cursor.Current.Speaker);
        Assert.IsTrue(cursor.MoveNext());
        Assert.AreEqual(DialogueSpeakerRole.Player, cursor.Current.Speaker);
        Assert.IsFalse(cursor.MoveNext());
        Assert.IsNull(cursor.Current);
    }

    [Test]
    public void BubbleSizing_ClampsWidthAndBodyToFourLines()
    {
        DialogueBubbleSize size = DialogueBubbleSizing.Calculate(
            new Vector2(900f, 500f),
            new Vector2(120f, 30f),
            40f,
            4,
            240f,
            600f,
            new Vector2(24f, 16f),
            8f);

        Assert.AreEqual(600f, size.FrameWidth);
        Assert.AreEqual(552f, size.BodyWidth);
        Assert.AreEqual(160f, size.BodyHeight);
        Assert.AreEqual(230f, size.FrameHeight);
    }

    [Test]
    public void BubbleSizing_ShortTextKeepsMinimumReadableWidth()
    {
        DialogueBubbleSize size = DialogueBubbleSizing.Calculate(
            new Vector2(30f, 40f),
            Vector2.zero,
            40f,
            4,
            240f,
            600f,
            new Vector2(24f, 16f),
            8f);

        Assert.AreEqual(240f, size.FrameWidth);
        Assert.AreEqual(192f, size.BodyWidth);
        Assert.AreEqual(72f, size.FrameHeight);
    }
}
#endif
