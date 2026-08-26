public static class EchoAudioFeedback
{
    public static bool TryGetCue(EchoData echo, EchoAudioMoment moment, out EchoAudioCue cue)
    {
        cue = default;
        if (echo == null) return false;

        cue = echo.GetAudioCue(moment);
        return cue.IsConfigured;
    }

    /// <summary>
    /// Plays through the existing global SFX pool and mixer. Returns true only
    /// when a configured cue reached an active SoundManager.
    /// </summary>
    public static bool Play(EchoData echo, EchoAudioMoment moment)
    {
        if (!TryGetCue(echo, moment, out EchoAudioCue cue)) return false;
        if (SoundManager.Instance == null) return false;

        SoundManager.Instance.PlaySFX(cue.clip, cue.SafeVolume, cue.randomizePitch);
        return true;
    }
}
