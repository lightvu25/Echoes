using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PausedUI : MonoBehaviour, IUIPanel
{
    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Audio Settings")]
    [SerializeField] private Slider soundVolumeSlider;
    [SerializeField] private TextMeshProUGUI soundVolumeTextMesh;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeTextMesh;

    private int lastInteractionFrame = -1;

    private void Awake()
    {
        // --- Setup Audio Sliders ---
        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.onValueChanged.AddListener((value) => {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.SetSoundVolume((int)value);
                    if (soundVolumeTextMesh != null)
                        soundVolumeTextMesh.text = "SOUND " + SoundManager.Instance.GetSoundVolume();
                }
            });
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener((value) => {
                if (MusicManager.Instance != null)
                {
                    MusicManager.Instance.SetMusicVolume((int)value);
                    if (musicVolumeTextMesh != null)
                        musicVolumeTextMesh.text = "MUSIC " + MusicManager.Instance.GetMusicVolume();
                }
            });
        }

        // --- Setup Buttons ---
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() =>
            {
                if (UIManager.Instance != null) UIManager.Instance.ClosePanelIfOpen(UIPanelType.Pause);
                else Hide();
                
                if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() =>
            {
                Application.Quit();
            });
        }
    }

    private void Start()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnMenuButtonPressed += GameInput_OnMenuButtonPressed;
            GameInput.Instance.OnCancelPressed += GameInput_OnCancelPressed;
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
        HandlePauseToggle();
    }

    private void GameInput_OnCancelPressed()
    {
        HandlePauseToggle();
    }

    private void HandlePauseToggle()
    {
        if (UIManager.Instance != null && UIManager.Instance.WasPanelClosedThisFrame) return;
        if (Time.frameCount == lastInteractionFrame) return;
        lastInteractionFrame = Time.frameCount;

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
        
        if (SoundManager.Instance != null && soundVolumeSlider != null)
        {
            soundVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.GetSoundVolume());
            if (soundVolumeTextMesh != null) soundVolumeTextMesh.text = "SOUND " + SoundManager.Instance.GetSoundVolume();
        }

        if (MusicManager.Instance != null && musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(MusicManager.Instance.GetMusicVolume());
            if (musicVolumeTextMesh != null) musicVolumeTextMesh.text = "MUSIC " + MusicManager.Instance.GetMusicVolume();
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);

        if (SoundManager.Instance != null) SoundManager.Instance.SaveVolume();
        if (MusicManager.Instance != null) MusicManager.Instance.SaveVolume();
    }
}