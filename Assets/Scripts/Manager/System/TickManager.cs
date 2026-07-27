using System;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public static float tickTime = 0.2f;

    private float _tickerTimer;

    public static event Action onTick;

    public static TickManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

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
