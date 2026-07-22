using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    private const int SOUND_VOLUME_MAX = 10;

    public static SoundManager Instance { get; private set; }

    private static int soundVolume = 6;
    private const string PREF_KEY = "SoundVolume";

    public event EventHandler OnSoundVolumeChanged;

    [Header("Mixer Routing")]
    public AudioMixer audioMixer;
    public AudioMixerGroup sfxMixerGroup;
    public string sfxVolumeParam = "SFXVolume";

    [Header("SFX Pooling")]
    [SerializeField] private int initialPoolSize = 10;
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private Transform poolContainer;

    [Header("Global Sounds")]
    [SerializeField] private AudioClip evolutionSound;

    private void Awake()
    {
        Instance = this;
        
        // Load volume from PlayerPrefs
        soundVolume = PlayerPrefs.GetInt(PREF_KEY, 6);
        
        InitializePool();
    }

    private void Start()
    {
        ApplyMixerVolume();
    }

    private void InitializePool()
    {
        poolContainer = new GameObject("SFX_Pool").transform;
        poolContainer.SetParent(transform);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        GameObject go = new GameObject("PooledAudioSource");
        go.transform.SetParent(poolContainer);
        AudioSource source = go.AddComponent<AudioSource>();
        
        // Setup for 2D generic sounds
        source.spatialBlend = 0f; 
        source.playOnAwake = false;
        
        if (sfxMixerGroup != null)
            source.outputAudioMixerGroup = sfxMixerGroup;

        sfxPool.Add(source);
        return source;
    }

    private AudioSource GetAvailableAudioSource()
    {
        foreach (var source in sfxPool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        
        // If all are playing, expand the pool
        return CreateNewAudioSource();
    }

    /// <summary>
    /// Plays a global 2D SFX (e.g., UI, Game Over, Item Pickups).
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, bool randomPitch = true)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableAudioSource();
        source.clip = clip;
        source.volume = volume;

        if (randomPitch)
            source.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        else
            source.pitch = 1f;

        source.Play();
    }

    public void PlayEvolutionSound()
    {
        if (evolutionSound != null)
        {
            PlaySFX(evolutionSound, 1f, false);
        }
    }

    public void ChangeSoundVolume()
    {
        SetSoundVolume((soundVolume + 1) % SOUND_VOLUME_MAX);
    }

    public void SetSoundVolume(int newVolume)
    {
        soundVolume = Mathf.Clamp(newVolume, 0, SOUND_VOLUME_MAX);
        ApplyMixerVolume();
        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetInt(PREF_KEY, soundVolume);
        PlayerPrefs.Save();
    }

    private void ApplyMixerVolume()
    {
        if (audioMixer != null)
        {
            float normalizedVolume = GetSoundVolumeNormalized();
            float dbValue = Mathf.Log10(Mathf.Max(0.0001f, normalizedVolume)) * 20f;
            audioMixer.SetFloat(sfxVolumeParam, dbValue);
        }
    }

    public int GetSoundVolume()
    {
        return soundVolume;
    }

    public float GetSoundVolumeNormalized()
    {
        return ((float)soundVolume) / SOUND_VOLUME_MAX;
    }
}
