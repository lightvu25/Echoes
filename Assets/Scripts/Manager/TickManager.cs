using System;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public static float tickTime = 0.2f;

    private float _tickerTimer;

    public static event Action onTick;

    private void Update()
    {
        _tickerTimer += Time.deltaTime;
        if (_tickerTimer >= tickTime)
        {
            _tickerTimer -= tickTime;
            onTick?.Invoke();
        }
    }
}
