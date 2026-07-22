using System;
using UnityEngine;

public class MetaProgressionManager : MonoBehaviour
{
    public static MetaProgressionManager Instance { get; private set; }

    public event Action<MindGardenNodeData> OnSkillUnlocked;
    public event Action OnCurrencyChanged;
    public event Action OnDataChanged;
    public event Action<string> OnUnlockFailed;

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

    public void RequestUnlock(MindGardenNodeData skill)
    {
        if (skill == null)
        {
            OnUnlockFailed?.Invoke("Skill data is null.");
            return;
        }

        if (GameSession.Instance == null || GameSession.Instance.currentProfile == null)
        {
            Debug.LogError("[MetaProgressionManager] GameSession or currentProfile is null.");
            OnUnlockFailed?.Invoke("System error: GameSession not found.");
            return;
        }

        ProfileData profile = GameSession.Instance.currentProfile;

        if (profile.HasSkill(skill.SkillID))
        {
            Debug.LogWarning($"[MetaProgressionManager] Skill '{skill.SkillID}' is already unlocked.");
            OnUnlockFailed?.Invoke("Skill is already unlocked.");
            return;
        }

        if (!skill.ArePrerequisitesMet(profile.unlockedSkillIDs))
        {
            Debug.LogWarning($"[MetaProgressionManager] Prerequisites for '{skill.SkillID}' not met.");
            OnUnlockFailed?.Invoke("Prerequisites not met.");
            return;
        }

        int invested = profile.GetInvestedAmount(skill.SkillID);
        int remainingCost = skill.MemoryCost - invested;

        if (PlayerStats.Instance == null || PlayerStats.Instance.CurrentAstralShards < remainingCost)
        {
            Debug.LogWarning($"[MetaProgressionManager] Not enough Astral Shards.");
            OnUnlockFailed?.Invoke("Not enough Astral Shards.");
            return;
        }

        if (remainingCost <= 0 || PlayerStats.Instance.SpendAstralShards(remainingCost))
        {
            profile.SetInvestedAmount(skill.SkillID, 0); // clear partial investment
            profile.unlockedSkillIDs.Add(skill.SkillID);
            SaveManager.saveProfile(profile);

            Debug.Log($"[MetaProgressionManager] '{skill.SkillName}' unlocked.");
            
            OnSkillUnlocked?.Invoke(skill);
            OnCurrencyChanged?.Invoke();
            OnDataChanged?.Invoke();
        }
        else
        {
            OnUnlockFailed?.Invoke("Failed to spend Astral Shards.");
        }
    }

    public void BuyPermanentMaxHP(int hpAmount, int cost)
    {
        if (cost <= 0) return;
        
        if (GameSession.Instance == null || GameSession.Instance.currentProfile == null)
        {
            Debug.LogError("[MetaProgressionManager] GameSession or currentProfile is null.");
            return;
        }

        if (PlayerStats.Instance == null || PlayerStats.Instance.CurrentAstralShards < cost)
        {
            Debug.LogWarning("[MetaProgressionManager] Not enough Astral Shards to buy Max HP.");
            return;
        }

        if (PlayerStats.Instance.SpendAstralShards(cost))
        {
            GameSession.Instance.currentProfile.bonusStartingMaxHP += hpAmount;
            SaveManager.saveProfile(GameSession.Instance.currentProfile);
            
            Debug.Log($"[MetaProgressionManager] Bought {hpAmount} Max HP for {cost} shards.");
            
            OnCurrencyChanged?.Invoke();
            OnDataChanged?.Invoke();
        }
    }

    public bool HasSkill(string skillID)
    {
        if (GameSession.Instance != null && GameSession.Instance.currentProfile != null)
        {
            return GameSession.Instance.currentProfile.HasSkill(skillID);
        }
        return false;
    }
}
