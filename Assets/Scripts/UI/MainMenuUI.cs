using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        // Evaluate the state of the "Continue" button
        var currentRun = SaveManager.loadRun();
        continueButton.interactable = currentRun != null;
    }

    private void Awake()
    {
        // 1. New Game button logic
        newGameButton.onClick.AddListener(() =>
        {
            SaveManager.deleteRun();
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        });

        // 2. Continue button logic
        continueButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
        });

        // 3. Quit button logic
        quitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }
}