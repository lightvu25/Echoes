using System;
using UnityEngine;

public class StatueInteractable : MonoBehaviour
{
    public event Action<int, int> OnStatueOpened;

    public void Interact()
    {
        if (PlayerStats.Instance != null && GameSession.Instance != null)
        {
            int runMems = PlayerStats.Instance.MemoryFragments;
            int bankedMems = GameSession.Instance.currentProfile.bankedMems;
            OnStatueOpened?.Invoke(runMems, bankedMems);
        }
    }

    public void BankMems(int amount)
    {
        if (PlayerStats.Instance != null && GameSession.Instance != null && amount > 0)
        {
            if (PlayerStats.Instance.MemoryFragments >= amount)
            {
                PlayerStats.Instance.SpendMemoryFragments(amount);
                GameSession.Instance.currentProfile.bankedMems += amount;
                SaveManager.saveProfile(GameSession.Instance.currentProfile);
            }
        }
    }

    public void WithdrawMems(int amount)
    {
        if (PlayerStats.Instance != null && GameSession.Instance != null && amount > 0)
        {
            if (GameSession.Instance.currentProfile.bankedMems >= amount)
            {
                GameSession.Instance.currentProfile.bankedMems -= amount;
                SaveManager.saveProfile(GameSession.Instance.currentProfile);

                PlayerStats.Instance.AddMemoryFragments(amount);
            }
        }
    }

    public void BuyPermanentMaxHP(int hpAmount, int cost)
    {
        if (GameSession.Instance != null && GameSession.Instance.currentProfile.bankedMems >= cost)
        {
            GameSession.Instance.currentProfile.bankedMems -= cost;
            GameSession.Instance.currentProfile.bonusStartingMaxHP += hpAmount;
            SaveManager.saveProfile(GameSession.Instance.currentProfile);
        }
    }

    public int GetBankedMems()
    {
        return GameSession.Instance != null ? GameSession.Instance.currentProfile.bankedMems : 0;
    }

    public int GetBonusStartingMaxHP()
    {
        return GameSession.Instance != null ? GameSession.Instance.currentProfile.bonusStartingMaxHP : 0;
    }
}
