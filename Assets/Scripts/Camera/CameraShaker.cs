using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;

    private CinemachineCamera _virtualCamera;
    private CinemachineBasicMultiChannelPerlin _cameraNoise;

    private const float SHAKE_STRENGTH = 1.0f;

    private void Start()
    {
        _virtualCamera = FindObjectOfType<CinemachineCamera>();
        _cameraNoise = _virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    #region Basic Shake
    public void BasicShake(float intensity, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(DoBasicShake(intensity, duration));
    }

    private IEnumerator DoBasicShake(float intensity, float duration)
    {
        _cameraNoise.AmplitudeGain = intensity * SHAKE_STRENGTH;
        yield return new WaitForSeconds(duration);
        _cameraNoise.AmplitudeGain = 0;
    }
    #endregion

    #region Smooth Shake
    public void SmoothShake(float intensity, float duration, float fadeInTime = 0, float fadeOutTime = 0)
    {
        StopAllCoroutines();
        StartCoroutine(DoSmoothShake(intensity, duration, fadeInTime, fadeOutTime));
    }

    private IEnumerator DoSmoothShake(float intensity, float duration, float fadeInTime, float fadeOutTime)
    {
        float startTime = Time.time;
        _cameraNoise.AmplitudeGain = 0;

        //Fade In
        while (Time.time - startTime < fadeInTime)
        {
            float fadeAmount = Mathf.Lerp(0, intensity, (Time.time - startTime) / fadeInTime);
            _cameraNoise.AmplitudeGain = fadeAmount * intensity * SHAKE_STRENGTH;
            yield return null;
        }

        //Main Shake
        _cameraNoise.AmplitudeGain = intensity * SHAKE_STRENGTH;
        yield return new WaitForSeconds(duration);

        //Fade Out
        while (Time.time - startTime < fadeInTime + duration + fadeOutTime)
        {
            float fadeAmount = Mathf.Lerp(intensity, 0, (Time.time - (startTime + fadeInTime + duration)) / fadeInTime);
            _cameraNoise.AmplitudeGain = fadeAmount * intensity * SHAKE_STRENGTH;
            yield return null;
        }

        _cameraNoise.AmplitudeGain = 0;
    }
    #endregion
}
