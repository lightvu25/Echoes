using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PassUI : MonoBehaviour, IUIPanel
{
    [SerializeField] private TextMeshProUGUI titleTextMesh;
    [SerializeField] private TextMeshProUGUI playButtonTextMesh;
    [SerializeField] private Button playButton;

    private Action playButtonClickAction;

    private void Start()
    {
        PlayerInteract.Instance.OnGoal += PlayerInteract_OnGoal;
        // PlayerInteract.Instance.OnDead += PlayerInteract_OnDead;
        Hide();
    }

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            playButtonClickAction();
        });
    }

    private void PlayerInteract_OnGoal(object sender, PlayerInteract.OnGoalEventArgs e)
    {
        titleTextMesh.text = "YOU ESCAPED";
        playButtonTextMesh.text = "CONTINUE";
        playButtonClickAction = GameManager.Instance.GoToNextLevel;
        
        if (UIManager.Instance != null)
            UIManager.Instance.OpenPanel(UIPanelType.Pass);
        else
            Show();
    }

    private void PlayerInteract_OnDead(object sender, EventArgs e)
    {
        titleTextMesh.text = "YOU ARE DEAD";
        playButtonTextMesh.text = "WAKE UP";
        playButtonClickAction = () => GameSession.Instance.HandlePlayerDeath();
        
        if (UIManager.Instance != null)
            UIManager.Instance.OpenPanel(UIPanelType.Pass);
        else
            Show();
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
