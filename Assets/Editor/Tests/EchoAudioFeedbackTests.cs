#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class EchoAudioFeedbackTests
{
    private EchoData echo;
    private AudioClip activationClip;
    private AudioClip attackClip;
    private AudioClip hitClip;

    [SetUp]
    public void SetUp()
    {
        echo = ScriptableObject.CreateInstance<EchoData>();
        activationClip = AudioClip.Create("Activation", 32, 1, 44100, false);
        attackClip = AudioClip.Create("Attack", 32, 1, 44100, false);
        hitClip = AudioClip.Create("Hit", 32, 1, 44100, false);

        echo.activationAudio = new EchoAudioCue(activationClip, 0.5f, false);
        echo.attackAudio = new EchoAudioCue(attackClip, 0.7f, true);
        echo.hitAudio = new EchoAudioCue(hitClip, 0.9f, true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(echo);
        Object.DestroyImmediate(activationClip);
        Object.DestroyImmediate(attackClip);
        Object.DestroyImmediate(hitClip);
    }

    [TestCase(EchoAudioMoment.Activation, 0.5f, false)]
    [TestCase(EchoAudioMoment.Attack, 0.7f, true)]
    [TestCase(EchoAudioMoment.Hit, 0.9f, true)]
    public void CueSelection_ReturnsTheConfiguredMoment(
        EchoAudioMoment moment,
        float expectedVolume,
        bool expectedRandomPitch)
    {
        Assert.IsTrue(EchoAudioFeedback.TryGetCue(echo, moment, out EchoAudioCue cue));
        Assert.AreEqual(echo.GetAudioCue(moment).clip, cue.clip);
        Assert.AreEqual(expectedVolume, cue.SafeVolume);
        Assert.AreEqual(expectedRandomPitch, cue.randomizePitch);
    }

    [Test]
    public void MissingClip_DoesNotReportAPlayableCue()
    {
        echo.hitAudio = new EchoAudioCue(null, 1f, true);

        Assert.IsFalse(EchoAudioFeedback.TryGetCue(echo, EchoAudioMoment.Hit, out _));
    }
}
#endif
