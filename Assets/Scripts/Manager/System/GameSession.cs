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
            Destroy(gameObject);
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
        if (savedRun != null)
        {
            currentRun = savedRun;
        }
        else
        {
            StartNewRun();
        }
    }
    public void StartNewRun()
    {
        Debug.Log("[GameSession] StartNewRun called.");
        currentRun = new RunData();
        currentRun.maxHealth = 100;
        currentRun.currentHealth = 100;
        currentRun.mapSeed = Random.Range(0, 999999);
        currentRun.levelNumber = 1;
    }
    public void HandlePlayerDeath()
    {
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
        StartCoroutine(HandlePlayerDeathSequence());
    }

    private IEnumerator HandlePlayerDeathSequence()
    {
        int lostShards = currentRun.runGold;

        CutsceneManager cutsceneManager = FindFirstObjectByType<CutsceneManager>();
        if (cutsceneManager != null)
        {
            yield return StartCoroutine(cutsceneManager.PlayDeathSequence(lostShards));
        }
        else
        {
            // Fallback if no CutsceneManager is in the scene
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
}
