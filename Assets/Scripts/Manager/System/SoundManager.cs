using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private const int SOUND_VOLUME_MAX = 10;

    public static SoundManager Instance { get; private set; }

    private static int soundVolume = 6;

    public event EventHandler OnSoundVolumeChanged;

    [SerializeField] private AudioClip coinPickupAudioClip;
    [SerializeField] private AudioClip timePickupAudioClip;
    [SerializeField] private AudioClip failAudioClip;
    [SerializeField] private AudioClip successAudioClip;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PlayerInteract.Instance.OnCoinPickup += PlayerInteract_OnCoinPickup;
        PlayerInteract.Instance.OnTimePickup += PlayerInteract_OnTimePickup;
        PlayerInteract.Instance.OnGoal += PlayerInteract_OnGoal;
        PlayerInteract.Instance.OnDead += PlayerInteract_OnDead;
    }

    private void PlayerInteract_OnGoal(object sender, PlayerInteract.OnGoalEventArgs e)
    {
        if (e.passing)
        {
            AudioSource.PlayClipAtPoint(successAudioClip, Camera.main.transform.position, GetSoundVolumeNormalized());
        }
        else
        {
            AudioSource.PlayClipAtPoint(failAudioClip, Camera.main.transform.position, GetSoundVolumeNormalized());
        }
    }

    private void PlayerInteract_OnCoinPickup(object sender, System.EventArgs e)
    {
        AudioSource.PlayClipAtPoint(coinPickupAudioClip, Camera.main.transform.position, GetSoundVolumeNormalized());
    }

    private void PlayerInteract_OnTimePickup(object sender, System.EventArgs e)
    {
        AudioSource.PlayClipAtPoint(timePickupAudioClip, Camera.main.transform.position, GetSoundVolumeNormalized());
    }

    private void PlayerInteract_OnDead(object sender, System.EventArgs e)
    {
        // You can add a death sound effect here if desired
    }

    public void ChangeSoundVolume()
    {
        soundVolume = (soundVolume + 1) % SOUND_VOLUME_MAX;
        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
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
