using UnityEngine;
using System.Collections;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    public void Awake()
    {
        Instance = this;
    }

    public void DoHitStop(float duration)
    {
        if (duration > 0)
        {
            StopAllCoroutines();
            StartCoroutine(HitStopCoroutine(duration));
        }
    }

    private IEnumerator HitStopCoroutine(float duration)
    {
        Time.timeScale = 0.0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }
}
