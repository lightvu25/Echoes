using System;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private AudioSource jumpAudioSource;
    [SerializeField] private AudioSource landAudioSource;
    [SerializeField] private AudioSource dashAudioSource;

    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        playerMovement.OnJump += Player_Jump;
        playerMovement.OnLand += Player_Land;
        playerMovement.OnWalk += Player_Walk;
        playerMovement.OnDash += Player_Dash;

        playerMovement.OnStopWalk += Player_StopWalk;

        SoundManager.Instance.OnSoundVolumeChanged += SoundManager_OnSoundVolumeChanged;

        UpdateAllVolumes();
    }

    private void UpdateAllVolumes()
    {
        float volume = SoundManager.Instance.GetSoundVolumeNormalized();
        walkAudioSource.volume = volume;
        jumpAudioSource.volume = volume;
        landAudioSource.volume = volume;
        dashAudioSource.volume = volume;
    }

    private void SoundManager_OnSoundVolumeChanged(object sender, System.EventArgs e)
    {
        UpdateAllVolumes();
    }

    private void Player_Jump(object sender, System.EventArgs e)
    {
        if (!jumpAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
            jumpAudioSource.Play();
        }
    }

    private void Player_Land(object sender, System.EventArgs e)
    {
        if (!landAudioSource.isPlaying)
        {
            landAudioSource.Play();
        }
    }

    private void Player_Walk(object sender, System.EventArgs e)
    {
        if (!walkAudioSource.isPlaying)
        {
            walkAudioSource.Play();
        }
    }

    private void Player_Dash(object sender, System.EventArgs e)
    {
        if (!dashAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
            dashAudioSource.Play();
        }
    }

    private void Player_StopWalk(object sender, System.EventArgs e)
    {
        walkAudioSource.Stop();
    }
}
