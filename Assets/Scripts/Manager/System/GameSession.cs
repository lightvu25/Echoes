using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [SerializeField] private string hubWorldSceneName = "GameScene";

    public ProfileData currentProfile;
    public RunData currentRun;

    private void Awake()
    { 
        Debug.Log($"[GameSession] Awake called on {gameObject.name}");
        Instance = this;
        
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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var move = player.GetComponent<PlayerMovement>();
            if (move != null) move.enabled = false;
            
            var combat = player.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;
            var anim = player.GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger("Death");
        }
        
        yield return new WaitForSeconds(2.5f);
        
        SaveManager.deleteRun();
        
        StartNewRun();
        
        SceneManager.LoadScene(hubWorldSceneName);
    }

    public void SaveCurrentRun()
    {
        SaveManager.saveRun(currentRun);
    }
}
