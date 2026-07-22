using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

[System.Serializable]
public struct MusicTrack
{
    public string trackName;
    public AudioClip clip;
}

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambientSource;
    
    private const int MUSIC_VOLUME_MAX = 10;

    private static float musicTime;
    private static int musicVolume = 4;
    private const string PREF_KEY = "MusicVolume";

    public event EventHandler OnMusicVolumeChanged;

    [Header("Mixer Routing")]
    public AudioMixer audioMixer;
    public AudioMixerGroup musicMixerGroup;
    public string musicVolumeParam = "MusicVolume";

    [Header("Tracks")]
    [SerializeField] private MusicTrack[] tracks;

    private Coroutine musicCrossfadeCoroutine;
    private Coroutine ambientCrossfadeCoroutine;

    private void Awake()
    {
        Instance = this;

        // Load volume from PlayerPrefs
        musicVolume = PlayerPrefs.GetInt(PREF_KEY, 4);

        if (musicMixerGroup != null)
        {
            if (musicSource != null) musicSource.outputAudioMixerGroup = musicMixerGroup;
            if (ambientSource != null) ambientSource.outputAudioMixerGroup = musicMixerGroup;
        }

        if (musicSource != null) musicSource.time = musicTime;
    }

    private void Start()
    {
        ApplyMixerVolume();
        
        if (musicCrossfadeCoroutine == null && musicSource != null)
            musicSource.volume = 1f;
            
        if (ambientCrossfadeCoroutine == null && ambientSource != null)
            ambientSource.volume = 1f;
    }

    private void Update()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicTime = musicSource.time;
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

        if (musicSource.clip == clip) return;

        if (musicCrossfadeCoroutine != null)
            StopCoroutine(musicCrossfadeCoroutine);

        musicCrossfadeCoroutine = StartCoroutine(AnimateCrossfade(musicSource, clip, fadeDuration, isMusic: true));
    }

    public void PlayAmbient(string trackName, float fadeDuration = 0.5f)
    {
        AudioClip clip = GetClipFromName(trackName);
        if (clip == null)
        {
            Debug.LogWarning($"MusicManager: Ambient Track '{trackName}' not found.");
            return;
        }

        if (ambientSource.clip == clip) return;

        if (ambientCrossfadeCoroutine != null)
            StopCoroutine(ambientCrossfadeCoroutine);

        ambientCrossfadeCoroutine = StartCoroutine(AnimateCrossfade(ambientSource, clip, fadeDuration, isMusic: false));
    }

    private IEnumerator AnimateCrossfade(AudioSource source, AudioClip nextTrack, float fadeDuration, bool isMusic)
    {
        float halfDuration = fadeDuration / 2f;
        float startVolume = source.volume;
        float elapsed = 0f;

        // Fade out
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfDuration);
            yield return null;
        }
        source.volume = 0f;

        // Đổi nhạc
        source.clip = nextTrack;
        if (isMusic) musicTime = 0f;
        source.Play();

        // Fade in
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
            yield return null;
        }
        source.volume = 1f;

        if (isMusic) musicCrossfadeCoroutine = null;
        else ambientCrossfadeCoroutine = null;
    }

    public void ChangeMusicVolume() { SetMusicVolume((musicVolume + 1) % MUSIC_VOLUME_MAX); }
    public void SetMusicVolume(int newVolume)
    {
        musicVolume = Mathf.Clamp(newVolume, 0, MUSIC_VOLUME_MAX);
        ApplyMixerVolume();
        OnMusicVolumeChanged?.Invoke(this, EventArgs.Empty);
    }
    public void SaveVolume() { PlayerPrefs.SetInt(PREF_KEY, musicVolume); PlayerPrefs.Save(); }
    private void ApplyMixerVolume()
    {
        if (audioMixer != null)
        {
            float normalizedVolume = GetMusicVolumeNormalized();
            float dbValue = Mathf.Log10(Mathf.Max(0.0001f, normalizedVolume)) * 20f;
            audioMixer.SetFloat(musicVolumeParam, dbValue);
        }
    }
    public int GetMusicVolume() => musicVolume;
    public float GetMusicVolumeNormalized() => (float)musicVolume / MUSIC_VOLUME_MAX;

    public void DuckMusic(bool isDucking)
    {
        float targetVolume = isDucking ? 0.3f : 1f;
        musicSource.DOFade(targetVolume, 0.5f).SetUpdate(true);
    }
}