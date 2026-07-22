using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomEventTrigger : MonoBehaviour
{
    [Header("Camera Zoom")]
    public bool modifyCameraZoom;
    public float targetZoomSize = 14f;

    [Header("Ambient & Music Audio")]
    public bool overrideRoomAudio;
    public string roomMusicTrack;
    public string roomAmbientTrack;

    [Header("Exit Behavior")]
    public bool resetAudioOnExit = true;
    public string defaultMapMusic = "The Abyss";
    public string defaultAmbientTrack = "Abyss Ambient";

    private bool _zoomApplied;
    private bool _audioApplied;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (modifyCameraZoom && !_zoomApplied)
        {
            CinemachineCameraZoom2D.Instance?.SetTargetOrthographicSize(targetZoomSize);
            _zoomApplied = true;
        }

        if (overrideRoomAudio && !_audioApplied)
        {
            if (!string.IsNullOrEmpty(roomMusicTrack))
            {
                MusicManager.Instance?.PlayMusic(roomMusicTrack, 1f);
            }
            if (!string.IsNullOrEmpty(roomAmbientTrack))
            {
                MusicManager.Instance?.PlayAmbient(roomAmbientTrack, 1f);
            }
            _audioApplied = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_zoomApplied)
        {
            CinemachineCameraZoom2D.Instance?.SetNormalOrthographicSize();
            _zoomApplied = false;
        }

        if (_audioApplied && resetAudioOnExit)
        {
            if (!string.IsNullOrEmpty(defaultMapMusic))
            {
                MusicManager.Instance?.PlayMusic(defaultMapMusic, 1f);
            }
            if (!string.IsNullOrEmpty(defaultAmbientTrack))
            {
                MusicManager.Instance?.PlayAmbient(defaultAmbientTrack, 1f);
            }
            _audioApplied = false;
        }
    }
}