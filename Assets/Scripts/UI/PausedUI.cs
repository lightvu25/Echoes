using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PausedUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button soundVolumeButton;
    [SerializeField] TextMeshProUGUI soundVolumeTextMesh;
    [SerializeField] private Button musicVolumeButton;
    [SerializeField] TextMeshProUGUI musicVolumeTextMesh;

    private void Awake()
    {
        soundVolumeButton.onClick.AddListener(() => {
            SoundManager.Instance.ChangeSoundVolume();
            soundVolumeTextMesh.text = "SOUND" + SoundManager.Instance.GetSoundVolume();
        });
        musicVolumeButton.onClick.AddListener(() => {
            MusicManager.Instance.ChangeMusicVolume();
            musicVolumeTextMesh.text = "MUSIC" + MusicManager.Instance.GetMusicVolume();
        });

        Time.timeScale = 1f;

        resumeButton.onClick.AddListener(() =>
        {
            GameManager.Instance.ResumeGame();
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }

    private void Start()
    {
        GameManager.Instance.OnGamePaused += GameManager_OnGamePaused;
        GameManager.Instance.OnGameResume += GameManager_OnGameResume;

        soundVolumeTextMesh.text = "SOUND" + SoundManager.Instance.GetSoundVolume();
        musicVolumeTextMesh.text = "MUSIC" + MusicManager.Instance.GetMusicVolume();
        Hide();
    }

    private void GameManager_OnGamePaused(object sender, System.EventArgs e)
    {
        Show();
    }

    private void GameManager_OnGameResume(object sender, System.EventArgs e)
    {
        Hide(); 
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
