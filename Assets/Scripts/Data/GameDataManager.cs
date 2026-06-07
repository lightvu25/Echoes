using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton facade over GameSession.currentProfile for runtime unlock queries,
/// level-attempt tracking, and persistence. All persistence delegates to SaveManager.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    // Skill IDs — match whatever the Statue assigns in ProfileData.unlockedSkillIDs.
    private const string ID_DOUBLE_JUMP    = "Skill_DoubleJump";
    private const string ID_PLUNGE_ATTACK  = "Skill_PlungeAttack";

    public bool isDoubleJumpUnlocked   => Profile != null && Profile.HasSkill(ID_DOUBLE_JUMP);
    public bool isPlungeAttackUnlocked => Profile != null && Profile.HasSkill(ID_PLUNGE_ATTACK);

    private static ProfileData Profile => GameSession.Instance?.currentProfile;

    // ------------------------------------------------------------------ //
    //  Persistence Keys                                                    //
    // ------------------------------------------------------------------ //

    public HashSet<string> persistenceKeys = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (Profile != null && Profile.persistenceKeys != null)
        {
            foreach (var key in Profile.persistenceKeys)
                persistenceKeys.Add(key);
        }
    }

    public void AddPersistenceKey(string key)
    {
        if (string.IsNullOrEmpty(key) || persistenceKeys.Contains(key)) return;

        persistenceKeys.Add(key);
        
        if (Profile != null)
        {
            if (!Profile.persistenceKeys.Contains(key))
                Profile.persistenceKeys.Add(key);
                
            SaveManager.saveProfile(Profile);
        }
    }

    public bool HasPersistenceKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return persistenceKeys.Contains(key);
    }

    // ------------------------------------------------------------------ //
    //  Level Attempt Tracking                                              //
    // ------------------------------------------------------------------ //

    /// <summary>Increments the attempt count for a level and flushes the profile to disk.</summary>
    public void IncrementLevelAttempt(int levelIndex)
    {
        if (Profile == null) return;
        Profile.IncrementLevelAttempt(levelIndex);
        SaveManager.saveProfile(Profile);
    }

    /// <summary>Returns how many times the player has attempted the given level.</summary>
    public int GetLevelAttemptCount(int levelIndex) =>
        Profile != null ? Profile.GetLevelAttempts(levelIndex) : 0;

    // ------------------------------------------------------------------ //
    //  Debug / Testing                                                     //
    // ------------------------------------------------------------------ //

    [ContextMenu("Debug: Toggle Double Jump")]
    public void Debug_ToggleDoubleJump() => DebugToggle(ID_DOUBLE_JUMP);

    [ContextMenu("Debug: Toggle Plunge Attack")]
    public void Debug_TogglePlungeAttack() => DebugToggle(ID_PLUNGE_ATTACK);

    private void DebugToggle(string skillID)
    {
        if (Profile == null) { Debug.LogWarning("[GameDataManager] No active profile."); return; }

        if (Profile.HasSkill(skillID))
            Profile.unlockedSkillIDs.Remove(skillID);
        else
            Profile.unlockedSkillIDs.Add(skillID);

        SaveManager.saveProfile(Profile);
        Debug.Log($"[GameDataManager] {skillID} is now {(Profile.HasSkill(skillID) ? "UNLOCKED" : "LOCKED")}.");
    }
}
