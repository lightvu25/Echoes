using UnityEngine;

/// <summary>
/// Adds configurable pitch variation to an AudioSource each time a sound is played.
/// Public play methods can be called from code, UnityEvents, or Animation Events.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class RandomPitchAudio : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] randomClips;
    [SerializeField] private bool playOnEnable;

    [Header("Pitch Variation")]
    [SerializeField, Range(0.1f, 3f)] private float minimumPitch = 0.9f;
    [SerializeField, Range(0.1f, 3f)] private float maximumPitch = 1.1f;

    private bool hasStarted;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        TryApplyMixerRouting();
    }

    private void OnEnable()
    {
        TryApplyMixerRouting();

        if (playOnEnable && hasStarted)
        {
            PlayRandomClip();
        }
    }

    private void Start()
    {
        hasStarted = true;
        TryApplyMixerRouting();

        if (playOnEnable)
        {
            PlayRandomClip();
        }
    }

    /// <summary>Randomizes pitch and plays the clip currently assigned to the AudioSource.</summary>
    public void Play()
    {
        if (audioSource == null || audioSource.clip == null) return;

        TryApplyMixerRouting();
        RandomizePitch();
        audioSource.Play();
    }

    /// <summary>Replaces the current clip, randomizes pitch, and starts playback.</summary>
    public void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        TryApplyMixerRouting();
        audioSource.clip = clip;
        RandomizePitch();
        audioSource.Play();
    }

    /// <summary>Chooses one configured clip, randomizes pitch, and plays it.</summary>
    public void PlayRandomClip()
    {
        if (randomClips == null || randomClips.Length == 0) return;

        int startIndex = Random.Range(0, randomClips.Length);
        for (int i = 0; i < randomClips.Length; i++)
        {
            AudioClip clip = randomClips[(startIndex + i) % randomClips.Length];
            if (clip == null) continue;

            PlayClip(clip);
            return;
        }
    }

    /// <summary>Applies and returns a new pitch within the configured range.</summary>
    public float RandomizePitch()
    {
        if (audioSource == null) return 1f;

        float pitch = AudioPitchUtility.GetRandomPitch(minimumPitch, maximumPitch);
        audioSource.pitch = pitch;
        return pitch;
    }

    private void TryApplyMixerRouting()
    {
        if (audioSource != null
            && audioSource.outputAudioMixerGroup == null
            && SoundManager.Instance != null)
        {
            audioSource.outputAudioMixerGroup = SoundManager.Instance.sfxMixerGroup;
        }
    }

    private void OnValidate()
    {
        minimumPitch = Mathf.Clamp(minimumPitch, 0.1f, 3f);
        maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 3f);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
}
