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
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        
        Debug.Log("[GameManager] Awake called. Setting Instance.");
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (currentGenerator != null) 
        {
            // If starting directly in the scene (first run), OnSceneLoaded might have already run, 
            // or we might need to run it. SceneLoaded runs before Start, so we don't need to do anything here if we already initialized.
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (this != Instance) return;

        Debug.Log($"[GameManager] OnSceneLoaded called for scene: {scene.name}");
        
        // FIX: Ensure time is unpaused before initializing a new scene!
        // This prevents the player from being frozen if they transitioned while timeScale was 0.
        if (TimeManager.Instance != null) TimeManager.Instance.ClearAllPauses();
        else Time.timeScale = 1f;
        
        currentGenerator = FindFirstObjectByType<BaseLevelGenerator>();
        cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        StartCoroutine(InitializeSceneRoutine());
    }

    private IEnumerator InitializeSceneRoutine()
    {
        yield return null;

        CutsceneManager cutsceneManager = FindFirstObjectByType<CutsceneManager>();
        if (cutsceneManager != null)
        {
            // Ensure player inputs are unlocked when a new scene starts, 
            // even if initialization aborts early (e.g. in MindScene)
            if (GameInput.Instance != null)
                GameInput.Instance.SetInputsEnabled(true);
        }

        if (PlayerInteract.Instance == null) 
        {
            Debug.LogWarning("[GameManager] PlayerInteract.Instance is NULL! Skipping initialization.");
            yield break;
        }
        
        PlayerInteract.Instance.OnCoinPickup  -= Player_OnCoinPickup;
        PlayerInteract.Instance.OnStateChanged -= Player_OnStateChanged;

        PlayerInteract.Instance.OnCoinPickup  += Player_OnCoinPickup;
        PlayerInteract.Instance.OnStateChanged += Player_OnStateChanged;

        if (currentGenerator == null)
        {
            Debug.LogWarning("[GameManager] No BaseLevelGenerator assigned in this scene. Aborting level load.");
            yield break;
        }

        // --- Read pending memory node from Hub transition ---
        MemoryNodeData pendingNode = GameSession.Instance?.pendingNextNode;
        if (pendingNode != null)
        {
            Debug.Log($"[GameManager] Applying memory route: {pendingNode.nodeName} (ID: {pendingNode.nodeID})");
            
            // Reset modifiers
            if (GameSession.Instance.currentRun != null)
            {
                GameSession.Instance.currentRun.currentLevelRelicMultiplier = 1f;
                GameSession.Instance.currentRun.currentLevelEchoMultiplier = 1f;
                GameSession.Instance.currentRun.currentLevelEquipmentMultiplier = 1f;
            }

            if (pendingNode.mapModifiers != null && GameSession.Instance.currentRun != null)
            {
                foreach (var mod in pendingNode.mapModifiers)
                {
                    Debug.Log($"  Modifier: {mod.modifierName} | Type: {mod.type} | Value: {mod.value}");
                    switch (mod.type)
                    {
                        case MapModifier.ModifierType.RelicLootMultiplier:
                            GameSession.Instance.currentRun.currentLevelRelicMultiplier = mod.value;
                            break;
                        case MapModifier.ModifierType.EchoLootMultiplier:
                            GameSession.Instance.currentRun.currentLevelEchoMultiplier = mod.value;
                            break;
                        case MapModifier.ModifierType.EquipmentLootMultiplier:
                            GameSession.Instance.currentRun.currentLevelEquipmentMultiplier = mod.value;
                            break;
                    }
                }
            }
            GameSession.Instance.pendingNextNode = null;
        }

        int depth = 1;
        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            depth = GameSession.Instance.currentRun.levelNumber;
        }
        
        currentGenerator.GenerateMap(depth);

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.IncrementLevelAttempt(depth);
        }

        Transform spawn = currentGenerator.GetPlayerSpawnPoint();
        if (spawn != null)
        {
            StartCoroutine(SpawnPlayerSafely(spawn.position));
        }

        if (cinemachineCamera != null && PlayerInteract.Instance != null)
        {
            cinemachineCamera.Target.TrackingTarget = PlayerInteract.Instance.transform;
            if (CinemachineCameraZoom2D.Instance != null)
            {
                CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize();
            }
        }
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
        
        Debug.Log("Loading MindScene...");
        SceneManager.LoadScene("MindScene");
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

        yield return new WaitForSeconds(0.5f);

        if (rb != null) 
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
        }

        CutsceneManager cutsceneManager = FindFirstObjectByType<CutsceneManager>();
        if (cutsceneManager != null)
        {
            yield return StartCoroutine(cutsceneManager.PlayOpeningSequence());
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
        if (TimeManager.Instance != null) TimeManager.Instance.PauseTime("GameManager");
        else Time.timeScale = 0f;
        OnGamePaused?.Invoke(this, EventArgs.Empty);
    }

    public void ResumeGame()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.ResumeTime("GameManager");
        else Time.timeScale = 1f;
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

    private void Player_OnStateChanged(object sender, PlayerInteract.OnStateChangedEventArgs e)
    {
        if (e.state == PlayerInteract.State.Normal)
        {
            cinemachineCamera.Target.TrackingTarget = PlayerInteract.Instance.transform;
            CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize();
        }
    }


}