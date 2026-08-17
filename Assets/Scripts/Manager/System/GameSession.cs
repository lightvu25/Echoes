using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using System.Collections;
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [SerializeField] private string hubWorldSceneName = "GameScene";

    public ProfileData currentProfile;
    public RunData currentRun;

    [HideInInspector] public MemoryNodeData pendingNextNode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }
    
    private void Initialize()
    {
        currentProfile = SaveManager.loadProfile();
        RunData savedRun = SaveManager.loadRun();
        if (savedRun != null && IsResumableRun(savedRun))
        {
            currentRun = savedRun;
        }
        else
        {
            if (savedRun != null)
            {
                Debug.LogWarning(
                    "[GameSession] Discarding a completed/dead run save so the player does not " +
                    "spawn in a permanently dead state.");
                SaveManager.deleteRun();
            }

            StartNewRun();
        }
    }

    private static bool IsResumableRun(RunData run)
    {
        return run != null && (run.maxHealth <= 0 || run.currentHealth > 0);
    }
    public void StartNewRun()
    {
        Debug.Log("[GameSession] StartNewRun called.");
        currentRun = new RunData();
        currentRun.maxHealth = 100;
        currentRun.currentHealth = 100;
        currentRun.mapSeed = Random.Range(0, 999999);
        currentRun.levelNumber = 1;

        int startingGold = 0;
        if (MetaProgressionManager.Instance != null)
        {
            if (MetaProgressionManager.Instance.HasSkill("GOLD_RESERVE_1")) startingGold += 100;
            if (MetaProgressionManager.Instance.HasSkill("GOLD_RESERVE_2")) startingGold += 150;
            
            // Mind Garden Loot Bonuses
            if (MetaProgressionManager.Instance.HasSkill("LOOT_RELIC_1")) currentRun.bonusRelicChance += 0.05f;
            if (MetaProgressionManager.Instance.HasSkill("LOOT_ECHO_1")) currentRun.bonusEchoChance += 0.05f;
            if (MetaProgressionManager.Instance.HasSkill("LOOT_EQUIPMENT_1")) currentRun.bonusEquipmentChance += 0.05f;
        }
        currentRun.runGold = startingGold;
    }
    public void HandlePlayerDeath()
    {
        int lostShards = currentRun.currentAstralShards;

        currentProfile.totalGold += currentRun.runGold;
        currentProfile.deaths++;
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ResetRunCurrencies();
        }
        else
        {
            currentRun.runGold = 0;
        }
        SaveManager.saveProfile(currentProfile);
        StartCoroutine(HandlePlayerDeathSequence(lostShards));
    }

    private IEnumerator HandlePlayerDeathSequence(int lostShards)
    {
        CutsceneManager cutsceneManager = FindFirstObjectByType<CutsceneManager>();
        if (cutsceneManager != null)
        {
            yield return StartCoroutine(cutsceneManager.PlayDeathSequence(lostShards));
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        // --- Cleanup & Reset ---
        SaveManager.deleteRun();
        StartNewRun();
        SceneManager.LoadScene(hubWorldSceneName);
    }

    public void SaveCurrentRun()
    {
        SaveManager.saveRun(currentRun);
    }

    public void AbandonRun()
    {
        if (currentProfile != null && currentRun != null)
        {
            currentProfile.totalGold += currentRun.runGold;
            SaveManager.saveProfile(currentProfile);
        }
        
        SaveManager.deleteRun();
        StartNewRun();
        SceneManager.LoadScene(hubWorldSceneName);
    }

    public void CompleteRun()
    {
        if (currentProfile != null && currentRun != null)
        {
            currentProfile.totalGold += currentRun.runGold;
            SaveManager.saveProfile(currentProfile);
        }

        SaveManager.deleteRun();
        StartNewRun();
        SceneManager.LoadScene(hubWorldSceneName);
    }
}
