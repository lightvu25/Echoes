using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    public ProfileData currentProfile;
    public RunData currentRun;

    private void Awake()
    { 
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

            // TODO: Load the saved run state (e.g., load scene, set player stats, etc.)
        }
        else
        {
            StartNewRun();
        }
    }

    public void StartNewRun()
    {
        currentRun = new RunData();
        currentRun.maxHealth = 100;
        currentRun.currentHealth = 100;
        currentRun.mapSeed = Random.Range(0, 999999);
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
            currentRun.runMemoryFragments = 0;
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
        currentRun = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SaveCurrentRun()
    {
        SaveManager.saveRun(currentRun);
    }
}
