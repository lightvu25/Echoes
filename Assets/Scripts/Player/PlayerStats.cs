using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [SerializeField] private int baseExp = 100;
    [SerializeField] private int expIncreasePerLevel = 50;

    [SerializeField] public Transform collectPoint;

    // Events
    public event Action<int> OnLevelUp;
    public event Action<int> OnGoldChanged;
    public event Action<int> OnMemoryFragmentsChanged;
    public event Action<int, int> OnExpChanged;

    public int CurrentLevel => GameSession.Instance.currentRun.currentLevel;
    public int CurrentExp => GameSession.Instance.currentRun.currentExp;
    public int CurrentGold => GameSession.Instance.currentRun.runGold;
    public int MemoryFragments => GameSession.Instance.currentProfile.memoryFragments;

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

        GameSession.Instance.currentRun.runGold += amount;
        OnGoldChanged?.Invoke(CurrentGold);
        GameSession.Instance.SaveCurrentRun();
    }

    public void AddMemoryFragments(int amount)
    {
        if (amount <= 0) return;

        GameSession.Instance.currentProfile.memoryFragments += amount;
        OnMemoryFragmentsChanged?.Invoke(MemoryFragments);
        SaveManager.saveProfile(GameSession.Instance.currentProfile);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        if (GameSession.Instance == null || GameSession.Instance.currentRun == null)
        {
            Debug.LogError("PlayerStats.AddExp NullReference detected! You likely forgot to add the 'GameSession' Manager object to your scene, or there is no active run initialized.");
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