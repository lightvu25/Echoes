using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<GameLevel> gameLevelList;
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private int score;
    private float time;
    private bool isTimerActive;

    private static int levelNumber = 1;

    public event EventHandler OnGamePaused;
    public event EventHandler OnGameResume;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PlayerInteract.Instance.OnCoinPickup += Player_OnCoinPickup;
        PlayerInteract.Instance.OnGoal += Player_OnGoal;
        PlayerInteract.Instance.OnStateChanged += Player_OnStateChanged;

        GameInput.Instance.OnMenuButtonPressed += GameInput_OnMenuButtonPressed;
        LoadCurrentLevel();
    }

    private void GameInput_OnMenuButtonPressed(object sender, EventArgs e)
    {
        PauseResumeGame();
    }

    private void Player_OnStateChanged(object sender, PlayerInteract.OnStateChangedEventArgs e)
    {
        isTimerActive = e.state == PlayerInteract.State.Normal;

        if (e.state == PlayerInteract.State.Normal)
        {
            cinemachineCamera.Target.TrackingTarget = PlayerInteract.Instance.transform;
            CinemachineCameraZoom2D.Instance.SetNormalOrthographicSize();
        }
    }

    private void Update()
    {
        if (isTimerActive)
        {
            time += Time.deltaTime;
        }
    }

    private void LoadCurrentLevel()
    {
        foreach (GameLevel gameLevel in gameLevelList)
        {
            if (gameLevel.GetLevelNumber() == levelNumber)
            {
                GameLevel spawnedGameLevel = Instantiate(gameLevel, Vector3.zero, Quaternion.identity);
                PlayerInteract.Instance.transform.position = spawnedGameLevel.GetPlayerStartPosition();
                cinemachineCamera.Target.TrackingTarget = spawnedGameLevel.GetCameraStartTargetTransform();
                CinemachineCameraZoom2D.Instance.SetTargetOrthographicSize(spawnedGameLevel.GetZoomedOutOrthographicSize());
            }
        }
    }

    private GameLevel GetGameLevel(int levelNum)
    {
        foreach (GameLevel gameLevel in gameLevelList)
        {
            if (gameLevel.GetLevelNumber() == levelNum)
            {
                return gameLevel;
            }
        }
        return null;
    }

    public static void ResetStaticData()
    {
        levelNumber = 1;
    }

    private void Player_OnCoinPickup(object sender, System.EventArgs e)
    {
        AddScore(100);
    }

    private void Player_OnGoal(object sender, PlayerInteract.OnGoalEventArgs e)
    {
        Debug.Log("Score: " + score);
    }

    public void AddScore(int points)
    {
        score += points;
        Debug.Log("Score: " + score);
    }

    public int GetScore()
    {
        return score;
    }

    public float GetTime()
    {
        return time;
    }

    public int GetLevelNumber()
    {
        return levelNumber;
    }

    public void GoToNextLevel()
    {
        levelNumber++;
        SceneLoader.LoadScene(SceneLoader.Scene.GameScene);

        if (GetGameLevel(levelNumber) == null)
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameOverScene);
        }
        else
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        }
    }

    public void RetryLevel()
    {
        SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
    }

    public void PauseResumeGame()
    {
        if (Time.timeScale == 1f)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
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
}