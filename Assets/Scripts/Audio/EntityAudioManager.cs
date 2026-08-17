using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public struct SoundEntry
{
    public string id;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume;
}

[RequireComponent(typeof(AudioSource))]
public class EntityAudioManager : MonoBehaviour
{
    [Header("Mixer Routing (Optional)")]
    public AudioMixerGroup sfxMixerGroup;

    [Header("Entity Sounds")]
    public SoundEntry[] sounds;

    private AudioSource oneShotAudioSource;
    private AudioSource loopingAudioSource;
    private Dictionary<string, SoundEntry> soundDict;

    private void Awake()
    {
        oneShotAudioSource = GetComponent<AudioSource>();
        // Ensure it can be spatialized
        oneShotAudioSource.spatialBlend = 1f; 
        oneShotAudioSource.playOnAwake = false;

        // Create a secondary source for looping sounds like footsteps
        loopingAudioSource = gameObject.AddComponent<AudioSource>();
        loopingAudioSource.spatialBlend = 1f;
        loopingAudioSource.playOnAwake = false;
        loopingAudioSource.loop = true;

        soundDict = new Dictionary<string, SoundEntry>();
        foreach (var entry in sounds)
        {
            if (!string.IsNullOrEmpty(entry.id) && !soundDict.ContainsKey(entry.id))
            {
                soundDict.Add(entry.id, entry);
            }
        }
    }

    private void Start()
    {
        // Auto-route to the master SFX Mixer if one isn't manually assigned
        if (sfxMixerGroup == null && SoundManager.Instance != null && SoundManager.Instance.sfxMixerGroup != null)
        {
            sfxMixerGroup = SoundManager.Instance.sfxMixerGroup;
        }

        if (sfxMixerGroup != null)
        {
            oneShotAudioSource.outputAudioMixerGroup = sfxMixerGroup;
            loopingAudioSource.outputAudioMixerGroup = sfxMixerGroup;
        }
    }

    /// <summary>
    /// Plays a sound by its string ID. 
    /// Highly useful for Unity Animation Events which can pass a string parameter.
    /// </summary>
    public void PlaySound(string soundId)
    {
        if (soundDict.TryGetValue(soundId, out SoundEntry entry))
        {
            if (entry.clip == null) return;
            
            // Add slight random pitch to prevent ear fatigue
            oneShotAudioSource.pitch = AudioPitchUtility.GetRandomPitch();
            
            // Use PlayOneShot so sounds can overlap (e.g., footstep while attacking)
            oneShotAudioSource.PlayOneShot(entry.clip, entry.volume > 0 ? entry.volume : 1f);
        }
        else
        {
            Debug.LogWarning($"[EntityAudioManager] Sound ID '{soundId}' not found on {gameObject.name}.");
        }
    }

    /// <summary>
    /// Plays a sound via the global SoundManager so it won't be cut off if this GameObject is destroyed/disabled.
    /// </summary>
    public void PlaySoundGlobal(string soundId)
    {
        if (soundDict.TryGetValue(soundId, out SoundEntry entry))
        {
            if (entry.clip == null) return;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(entry.clip, entry.volume > 0 ? entry.volume : 1f, true);
            }
        }
        else
        {
            Debug.LogWarning($"[EntityAudioManager] Sound ID '{soundId}' not found on {gameObject.name}.");
        }
    }

    public void PlayLoopingSound(string soundId)
    {
        if (soundDict.TryGetValue(soundId, out SoundEntry entry))
        {
            if (entry.clip == null) return;
            if (loopingAudioSource.isPlaying && loopingAudioSource.clip == entry.clip) return;

            loopingAudioSource.clip = entry.clip;
            loopingAudioSource.volume = entry.volume > 0 ? entry.volume : 1f;
            loopingAudioSource.Play();
        }
        else
        {
            Debug.LogWarning($"[EntityAudioManager] Looping Sound ID '{soundId}' not found on {gameObject.name}.");
        }
    }

    public void StopLoopingSound()
    {
        if (loopingAudioSource.isPlaying)
        {
            loopingAudioSource.Stop();
        }
    }

    // Common Parameterless Methods for simpler Animation Events
    public void PlayAttackSound() => PlaySound("Attack");
    public void PlayHurtSound() => PlaySound("Hurt");
    public void PlayDieSound() => PlaySound("Die");
    public void PlayFootstepSound() => PlaySound("Footstep");
}
