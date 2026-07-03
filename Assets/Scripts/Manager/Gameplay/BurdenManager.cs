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
            PlayerStats.Instance.OnAstralShardsChanged += HandleAstralShardsChanged;
            UpdateMultipliers(PlayerStats.Instance.CurrentAstralShards);
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnAstralShardsChanged -= HandleAstralShardsChanged;
        }
    }

    private void HandleAstralShardsChanged(int newShards)
    {
        UpdateMultipliers(newShards);
    }

    private void UpdateMultipliers(int cores)
    {
        CurrentDamageMultiplier = 1f + (cores * damageScalePerCore);
        CurrentHealthMultiplier = 1f + (cores * healthScalePerCore);
        CurrentSpeedMultiplier = 1f + (cores * speedScalePerCore);
        CurrentDropRateMultiplier = 1f + (cores * dropRateScalePerCore);

        OnBurdenChanged?.Invoke(this, EventArgs.Empty);
    }
}
