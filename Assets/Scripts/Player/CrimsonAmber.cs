using UnityEngine;
using System;

public class CrimsonAmber : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int baseMaxAmbers = 1;
    [SerializeField] private float healPercentage = 0.4f;

    [Header("Skill Tree Settings")]
    [SerializeField] private string mindGardenUpgradeID = "HealingFlask_Upgrade";
    [SerializeField] private int extraUsesPerUpgrade = 1;

    public int CurrentAmbers { get; private set; }
    public int CurrentOrbs { get; private set; }
    
    public int MaxAmbers
    {
        get
        {
            int max = baseMaxAmbers;
            if (PlayerStats.Instance != null)
            {
                max += PlayerStats.Instance.BonusFlaskCount; 
            }
            if (GameSession.Instance != null && GameSession.Instance.currentProfile != null)
            {
                if (GameSession.Instance.currentProfile.HasSkill(mindGardenUpgradeID))
                {
                    max += extraUsesPerUpgrade;
                }
            }
            return max;
        }
    }

    public float CurrentHealPercentage
    {
        get
        {
            float percentage = healPercentage;
            if (PlayerStats.Instance != null)
            {
                percentage += PlayerStats.Instance.BonusFlaskHealPercentage;
            }
            return percentage;
        }
    }

    public event Action<int, int, int> OnAmberStateChanged;
    public event Action OnConsume;

    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null) healthSystem = GetComponentInParent<HealthSystem>();
    }

    private void Start()
    {
        CurrentAmbers = MaxAmbers;
        CurrentOrbs = 0;

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnHealPressed += HandleHealInput;
        }

        NotifyStateChanged();
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnHealPressed -= HandleHealInput;
        }
    }

    public void AddOrb()
    {
        CurrentOrbs++;
        if (CurrentOrbs >= 3)
        {
            CurrentOrbs -= 3;
            if (CurrentAmbers < MaxAmbers)
            {
                CurrentAmbers++;
                Debug.Log($"[CrimsonAmber] Orb threshold reached! Gained 1 Amber. Current: {CurrentAmbers}/{MaxAmbers}");
            }
        }
        NotifyStateChanged();
    }

    private void HandleHealInput()
    {
        if (CurrentAmbers <= 0)
        {
            Debug.Log("[CrimsonAmber] No ambers remaining!");
            return;
        }

        if (healthSystem == null || healthSystem.CurrentHP >= healthSystem.MaxHP)
        {
            Debug.Log("[CrimsonAmber] HP is already full!");
            return;
        }

        CurrentAmbers--;

        int healAmount = Mathf.RoundToInt(healthSystem.MaxHP * CurrentHealPercentage);
        if (healAmount < 1) healAmount = 1;

        healthSystem.Heal(healAmount);
        Debug.Log($"[CrimsonAmber] Used 1 amber to heal {healAmount} HP.");

        OnConsume?.Invoke();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnAmberStateChanged?.Invoke(CurrentAmbers, MaxAmbers, CurrentOrbs);
    }
}
