using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomEventTrigger : MonoBehaviour
{
    [Header("Camera Zoom")]
    [Tooltip("Override the camera's orthographic size while the player is inside.")]
    public bool modifyCameraZoom;
    public float targetZoomSize = 14f;

    // ------------------------------------------------------------------ //
    //  Future effects can be added here as additional [Header] sections:  //
    //                                                                      //
    //  [Header("Ambient Audio")]                                           //
    //  public bool overrideAmbientAudio;                                   //
    //  public AudioClip ambientClip;                                       //
    //                                                                      //
    //  [Header("Post Processing")]                                         //
    //  public bool enableHorrorProfile;                                    //
    // ------------------------------------------------------------------ //

    private bool _zoomApplied;

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
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_zoomApplied)
        {
            CinemachineCameraZoom2D.Instance?.SetNormalOrthographicSize();
            _zoomApplied = false;
        }
    }
}
