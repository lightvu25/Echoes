using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    private HashSet<string> pauseRequesters = new HashSet<string>();

    public void Awake()
    {
        Instance = this;
        ClearAllPauses(); // Ensure time scale is unpaused initially
    }

    public void PauseTime(string requesterId)
    {
        pauseRequesters.Add(requesterId);
        UpdateTimeScale();
    }

    public void ResumeTime(string requesterId)
    {
        pauseRequesters.Remove(requesterId);
        UpdateTimeScale();
    }

    public void ClearAllPauses()
    {
        pauseRequesters.Clear();
        UpdateTimeScale();
    }

    private void UpdateTimeScale()
    {
        Time.timeScale = pauseRequesters.Count > 0 ? 0f : 1f;
    }

    public void DoTemporaryPause(float duration, string requesterId)
    {
        if (duration > 0)
        {
            string uniqueId = requesterId + "_" + System.Guid.NewGuid().ToString();
            StartCoroutine(TemporaryPauseCoroutine(duration, uniqueId));
        }
    }

    private IEnumerator TemporaryPauseCoroutine(float duration, string requesterId)
    {
        PauseTime(requesterId);
        yield return new WaitForSecondsRealtime(duration);
        ResumeTime(requesterId);
    }

    // Maintained for backwards compatibility if needed, though replaced in most scripts
    public void DoHitStop(float duration)
    {
        DoTemporaryPause(duration, "HitStop");
    }
}
