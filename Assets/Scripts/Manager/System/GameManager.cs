using System;
using UnityEngine;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private CinemachineCamera cinemachineCamera;

    public BaseLevelGenerator currentGenerator;

    public event EventHandler OnGamePaused;
    public event EventHandler OnGameResume;

    private void Awake()
    {
        Debug.Log("[GameManager] Awake called. Setting Instance.");
        Instance = this;
    }

    private void Start()
    {
        Debug.Log("[GameManager] Start called.");
        if (PlayerInteract.Instance == null) Debug.LogError("[GameManager] PlayerInteract.Instance is NULL!");
        
        PlayerInteract.Instance.OnCoinPickup  += Player_OnCoinPickup;
        PlayerInteract.Instance.OnGoal        += Player_OnGoal;
        PlayerInteract.Instance.OnStateChanged += Player_OnStateChanged;



        if (currentGenerator == null)
        {
            Debug.LogError("[GameManager] No BaseLevelGenerator assigned. Aborting level load.");
            return;
        }

        int depth = GameSession.Instance.currentRun.levelNumber;
        currentGenerator.GenerateMap(depth);

        GameDataManager.Instance?.IncrementLevelAttempt(depth);

        Transform spawn = currentGenerator.GetPlayerSpawnPoint();
        if (spawn != null)
        {
            StartCoroutine(SpawnPlayerSafely(spawn.position));
        }

        cinemachineCamera.Target.TrackingTarget = PlayerInteract.Instance.transform;
        CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize();
    }

    // ------------------------------------------------------------------ //
    //  Level Progression (Roguelike in-scene regeneration)                 //
    // ------------------------------------------------------------------ //

    public void GoToNextLevel()
    {
        GameSession.Instance.currentRun.levelNumber++;

        currentGenerator.ClearMap();
        currentGenerator.GenerateMap(GameSession.Instance.currentRun.levelNumber);

        GameDataManager.Instance?.IncrementLevelAttempt(GameSession.Instance.currentRun.levelNumber);

        Transform spawn = currentGenerator.GetPlayerSpawnPoint();
        if (spawn != null)
        {
            StartCoroutine(SpawnPlayerSafely(spawn.position));
        }

        GameSession.Instance.SaveCurrentRun();
    }

    private System.Collections.IEnumerator SpawnPlayerSafely(Vector3 targetPosition)
    {
        var rb = PlayerInteract.Instance.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        PlayerInteract.Instance.transform.position = targetPosition;

        // Wait for TilemapMerger to finish merging all rooms
        if (RuntimeTilemapMerger.Instance != null)
        {
            while (RuntimeTilemapMerger.Instance.IsMerging)
            {
                yield return null;
            }
        }

        // Wait enough time to ensure composite colliders rebuild their geometry
        yield return new WaitForSeconds(0.5f);

        if (rb != null) 
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
        }
    }

    public int GetLevelNumber() => GameSession.Instance.currentRun.levelNumber;

    // ------------------------------------------------------------------ //
    //  Pause / Resume                                                      //
    // ------------------------------------------------------------------ //

    public void PauseResumeGame()
    {
        if (Time.timeScale == 1f) PauseGame();
        else                      ResumeGame();
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        OnGamePaused?.Invoke(this, EventArgs.Empty);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        OnGameResume?.Invoke(this, EventArgs.Empty);
    }

    // ------------------------------------------------------------------ //
    //  Event Handlers                                                      //
    // ------------------------------------------------------------------ //

    private void Player_OnCoinPickup(object sender, EventArgs e)
    {
        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            PlayerStats.Instance?.AddGold(1);
        }
    }

    private void Player_OnGoal(object sender, PlayerInteract.OnGoalEventArgs e)
    {
        GoToNextLevel();
    }

    private void Player_OnStateChanged(object sender, PlayerInteract.OnStateChangedEventArgs e)
    {
        if (e.state == PlayerInteract.State.Normal)
        {
            cinemachineCamera.Target.TrackingTarget = PlayerInteract.Instance.transform;
            CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize();
        }
    }


}