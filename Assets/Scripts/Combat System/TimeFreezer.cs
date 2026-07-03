using System.Collections;
using UnityEngine;

public class TimeFreezer : MonoBehaviour
{
    public static TimeFreezer Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FreezeTime(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(DoTimeFreeze(duration));
    }

    private IEnumerator DoTimeFreeze(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }
}