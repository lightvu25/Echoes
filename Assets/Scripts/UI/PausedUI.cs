using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PausedUI : MonoBehaviour, IUIPanel
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
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ChangeSoundVolume();
                if (soundVolumeTextMesh != null)
                    soundVolumeTextMesh.text = "SOUND" + SoundManager.Instance.GetSoundVolume();
            }
        });
        musicVolumeButton.onClick.AddListener(() => {
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.ChangeMusicVolume();
                if (musicVolumeTextMesh != null)
                    musicVolumeTextMesh.text = "MUSIC" + MusicManager.Instance.GetMusicVolume();
            }
        });

        Time.timeScale = 1f;

        resumeButton.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null) UIManager.Instance.ClosePanelIfOpen(UIPanelType.Pause);
            else Hide();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }

    private void Start()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnMenuButtonPressed += GameInput_OnMenuButtonPressed;
            GameInput.Instance.OnCancelPressed += GameInput_OnCancelPressed;
        }

        if (SoundManager.Instance != null && soundVolumeTextMesh != null)
        {
            soundVolumeTextMesh.text = "SOUND" + SoundManager.Instance.GetSoundVolume();
        }

        if (MusicManager.Instance != null && musicVolumeTextMesh != null)
        {
            musicVolumeTextMesh.text = "MUSIC" + MusicManager.Instance.GetMusicVolume();
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnMenuButtonPressed -= GameInput_OnMenuButtonPressed;
            GameInput.Instance.OnCancelPressed -= GameInput_OnCancelPressed;
        }
    }

    private void GameInput_OnMenuButtonPressed(object sender, System.EventArgs e)
    {
        if (UIManager.Instance != null && UIManager.Instance.WasPanelClosedThisFrame) return;

        if (gameObject.activeSelf)
        {
            if (UIManager.Instance != null) UIManager.Instance.ClosePanelIfOpen(UIPanelType.Pause);
            else Hide();
            
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }
        else
        {
            if (Time.timeScale > 0f)
            {
                if (UIManager.Instance != null)
                {
                    if (!UIManager.Instance.IsAnyPanelOpen)
                    {
                        UIManager.Instance.OpenPanel(UIPanelType.Pause);
                        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
                    }
                }
                else
                {
                    Show();
                    if (GameManager.Instance != null) GameManager.Instance.PauseGame();
                }
            }
        }
    }

    private void GameInput_OnCancelPressed()
    {
        if (UIManager.Instance != null && UIManager.Instance.WasPanelClosedThisFrame) return;

        if (gameObject.activeSelf)
        {
            if (UIManager.Instance != null) UIManager.Instance.ClosePanelIfOpen(UIPanelType.Pause);
            else Hide();
            
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }
        else
        {
            if (Time.timeScale > 0f)
            {
                if (UIManager.Instance != null)
                {
                    if (!UIManager.Instance.IsAnyPanelOpen)
                    {
                        UIManager.Instance.OpenPanel(UIPanelType.Pause);
                        if (GameManager.Instance != null) GameManager.Instance.PauseGame();
                    }
                }
                else
                {
                    Show();
                    if (GameManager.Instance != null) GameManager.Instance.PauseGame();
                }
            }
        }
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
