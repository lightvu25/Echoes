using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public struct MusicTrack
{
    public string trackName;
    public AudioClip clip;
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private const int MUSIC_VOLUME_MAX = 10;

    private static float musicTime;
    private static int musicVolume = 4;

    public event EventHandler OnMusicVolumeChanged;

    [SerializeField] private MusicTrack[] tracks;

    private AudioSource musicAudioSource;
    private Coroutine crossfadeCoroutine;

    private void Awake()
    {
        Instance = this;

        musicAudioSource = GetComponent<AudioSource>();
        musicAudioSource.time = musicTime;
    }

    private void Start()
    {
        musicAudioSource.volume = GetMusicVolumeNormalized();
    }

    private void Update()
    {
        if (musicAudioSource.isPlaying)
            musicTime = musicAudioSource.time;
    }

    public AudioClip GetClipFromName(string trackName)
    {
        foreach (var track in tracks)
        {
            if (track.trackName == trackName)
                return track.clip;
        }
        return null;
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        AudioClip clip = GetClipFromName(trackName);
        if (clip == null)
        {
            Debug.LogWarning($"MusicManager: Track '{trackName}' not found.");
            return;
        }

        if (musicAudioSource.clip == clip) return;

        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        crossfadeCoroutine = StartCoroutine(AnimateMusicCrossfade(clip, fadeDuration));
    }

    private IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration)
    {
        float halfDuration = fadeDuration / 2f;

        float startVolume = musicAudioSource.volume;
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
            yield return null;
        }
        musicAudioSource.volume = 0f;

        musicAudioSource.clip = nextTrack;
        musicTime = 0f;
        musicAudioSource.Play();

        elapsed = 0f;
        float targetVolume = GetMusicVolumeNormalized();
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            musicAudioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / halfDuration);
            yield return null;
        }
        musicAudioSource.volume = targetVolume;

        crossfadeCoroutine = null;
    }

    public void ChangeMusicVolume()
    {
        musicVolume = (musicVolume + 1) % MUSIC_VOLUME_MAX;
        if (crossfadeCoroutine == null)
            musicAudioSource.volume = GetMusicVolumeNormalized();
        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetMusicVolume() => musicVolume;

    public float GetMusicVolumeNormalized() => (float)musicVolume / MUSIC_VOLUME_MAX;
}
