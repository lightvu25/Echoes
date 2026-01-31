using System.Security.Cryptography;
using UnityEngine;

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

        SaveManager.saveProfile(currentProfile);
        SaveManager.deleteRun();
        currentRun = null;
    }

    public void SaveCurrentRun()
    {
        SaveManager.saveRun(currentRun);
    }
}
