using UnityEngine;
using System;

public class BurdenManager : MonoBehaviour
{
    public static BurdenManager Instance { get; private set; }

    public event EventHandler OnBurdenChanged;

    [Header("Scaling Settings")]
    [SerializeField] private float damageScalePerCore = 0.05f;
    [SerializeField] private float healthScalePerCore = 0.1f;
    [SerializeField] private float speedScalePerCore = 0.02f;
    [SerializeField] private float dropRateScalePerCore = 0.05f;

    [Header("Scaling Limits")]
    [SerializeField] private int maxBurdenLevel = 10;
    [SerializeField] private int goldBurdenBase = 500;
    [SerializeField] private int shardsPerBurden = 50;

    public float CurrentDamageMultiplier { get; private set; } = 1f;
    public float CurrentHealthMultiplier { get; private set; } = 1f;
    public float CurrentSpeedMultiplier { get; private set; } = 1f;
    public float CurrentDropRateMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnAstralShardsChanged += HandleCurrencyChanged;
            PlayerStats.Instance.OnGoldChanged += HandleCurrencyChanged;
            UpdateMultipliers();
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnAstralShardsChanged -= HandleCurrencyChanged;
            PlayerStats.Instance.OnGoldChanged -= HandleCurrencyChanged;
        }
    }

    private void HandleCurrencyChanged(int newValue)
    {
        UpdateMultipliers();
    }

    private void UpdateMultipliers()
    {
        if (PlayerStats.Instance == null) return;

        // Calculate burden level from peak currency
        int goldBurden = GetFibonacciBurdenLevel(PlayerStats.Instance.PeakGold);
        int shardBurden = shardsPerBurden > 0 ? PlayerStats.Instance.CurrentAstralShards / shardsPerBurden : 0;
        
        int totalBurden = Mathf.Min(maxBurdenLevel, goldBurden + shardBurden);

        CurrentDamageMultiplier = 1f + (totalBurden * damageScalePerCore);
        CurrentHealthMultiplier = 1f + (totalBurden * healthScalePerCore);
        CurrentSpeedMultiplier = 1f + (totalBurden * speedScalePerCore);
        CurrentDropRateMultiplier = 1f + (totalBurden * dropRateScalePerCore);

        OnBurdenChanged?.Invoke(this, EventArgs.Empty);
    }

    private int GetFibonacciBurdenLevel(int peakGold)
    {
        int level = 0;
        int a = 1;
        int b = 1;
        
        while (level < maxBurdenLevel)
        {
            int threshold = b * goldBurdenBase;
            if (peakGold < threshold) break;
            
            level++;
            int temp = a;
            a = b;
            b = temp + b;
        }
        return level;
    }
}
