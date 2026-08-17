using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [SerializeField] private int baseExp = 100;
    [SerializeField] private int expIncreasePerLevel = 50;

    [SerializeField] public Transform collectPoint;

    // Bonus stats from meta progression
    public int BonusFlaskCount { get; set; } = 0;
    public float BonusFlaskHealPercentage { get; set; } = 0f;

    // Events
    public event Action<int> OnLevelUp;
    public event Action<int> OnGoldChanged;
    public event Action<int> OnAstralShardsChanged;
    public event Action<int, int> OnExpChanged;

    private int testGold = 0;
    private int testAstralShards = 0;
    private int testExp = 0;
    private int testLevel = 1;

    public int CurrentLevel => GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.currentLevel : testLevel;
    public int CurrentExp => GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.currentExp : testExp;
    public int CurrentGold => GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.runGold : testGold;
    public int PeakGold => GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.peakRunGold : testGold;
    public int CurrentAstralShards => GameSession.Instance != null && GameSession.Instance.currentRun != null ? GameSession.Instance.currentRun.currentAstralShards : testAstralShards;

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

    public int GetRequiredExpForNextLevel()
    {
        return baseExp + (CurrentLevel - 1) * expIncreasePerLevel;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.runGold += amount;
            if (GameSession.Instance.currentRun.runGold > GameSession.Instance.currentRun.peakRunGold)
            {
                GameSession.Instance.currentRun.peakRunGold = GameSession.Instance.currentRun.runGold;
            }
            GameSession.Instance.SaveCurrentRun();
        }
        else
        {
            testGold += amount;
        }
        
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public void AddAstralShards(int amount)
    {
        if (amount <= 0) return;

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentAstralShards += amount;
            GameSession.Instance.SaveCurrentRun();
        }
        else
        {
            testAstralShards += amount;
        }

        OnAstralShardsChanged?.Invoke(CurrentAstralShards);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0 || CurrentGold < amount) return false;

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.runGold -= amount;
            GameSession.Instance.SaveCurrentRun();
        }
        else
        {
            testGold -= amount;
        }

        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }

    public bool SpendAstralShards(int amount)
    {
        if (amount <= 0 || CurrentAstralShards < amount) return false;

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentAstralShards -= amount;
            GameSession.Instance.SaveCurrentRun();
        }
        else
        {
            testAstralShards -= amount;
        }

        OnAstralShardsChanged?.Invoke(CurrentAstralShards);
        return true;
    }

    public void ResetRunCurrencies()
    {
        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.runGold = 0;
            GameSession.Instance.currentRun.currentAstralShards = 0;
            GameSession.Instance.SaveCurrentRun();
        }
        else
        {
            testGold = 0;
            testAstralShards = 0;
        }
        
        OnGoldChanged?.Invoke(0);
        OnAstralShardsChanged?.Invoke(0);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        if (GameSession.Instance == null || GameSession.Instance.currentRun == null)
        {
            testExp += amount;
            while (testExp >= GetRequiredExpForNextLevel())
            {
                testExp -= GetRequiredExpForNextLevel();
                testLevel++;
                OnLevelUp?.Invoke(CurrentLevel);
            }
            OnExpChanged?.Invoke(CurrentExp, GetRequiredExpForNextLevel());
            return;
        }

        GameSession.Instance.currentRun.currentExp += amount;

        while (GameSession.Instance.currentRun.currentExp >= GetRequiredExpForNextLevel())
        {
            GameSession.Instance.currentRun.currentExp -= GetRequiredExpForNextLevel();
            GameSession.Instance.currentRun.currentLevel++;
            OnLevelUp?.Invoke(CurrentLevel);
        }

        OnExpChanged?.Invoke(CurrentExp, GetRequiredExpForNextLevel());
        GameSession.Instance.SaveCurrentRun();
    }
}