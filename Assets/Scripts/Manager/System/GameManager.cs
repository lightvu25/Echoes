using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        // --- Read pending memory node from Hub transition ---
        MemoryNodeData pendingNode = GameSession.Instance?.pendingNextNode;
        if (pendingNode != null)
        {
            Debug.Log($"[GameManager] Applying memory route: {pendingNode.nodeName} (ID: {pendingNode.nodeID})");
            if (pendingNode.mapModifiers != null)
            {
                foreach (var mod in pendingNode.mapModifiers)
                {
                    Debug.Log($"  Modifier: {mod.modifierName} | Type: {mod.type} | Value: {mod.value}");
                }
            }
            // Clear after reading — modifiers will be wired into
            // EvolutionManager/BurdenManager in a future step.
            GameSession.Instance.pendingNextNode = null;
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

    // ------------------------------------------------------------------ //
    //  Hub Scene Transition                                                //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Called when the player reaches a Goal. Disables player control,
    /// saves the run, and starts the transition to HubScene.
    /// </summary>
    public void TriggerLevelTransition(Vector3 goalPos)
    {
        Debug.Log("[GameManager] Level transition triggered — heading to Hub.");

        // Disable player input
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var move = player.GetComponent<PlayerMovement>();
            if (move != null) move.enabled = false;

            var combat = player.GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;
        }

        GameSession.Instance.SaveCurrentRun();

        StartCoroutine(TransitionToHub(goalPos));
    }

    private IEnumerator TransitionToHub(Vector3 goalPos)
    {
        CutsceneManager cutsceneManager = FindFirstObjectByType<CutsceneManager>();
        if (cutsceneManager != null)
        {
            yield return StartCoroutine(cutsceneManager.PlayGoalSequence(goalPos));
        }
        
        Debug.Log("[GameManager] Loading HubScene...");
        SceneManager.LoadScene("HubScene");
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
        Vector3 goalPos = e.goal != null ? e.goal.transform.position : PlayerInteract.Instance.transform.position;
        TriggerLevelTransition(goalPos);
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