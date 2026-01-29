using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PassUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleTextMesh;
    [SerializeField] private TextMeshProUGUI playButtonTextMesh;
    [SerializeField] private Button playButton;

    private Action playButtonClickAction;

    private void Start()
    {
        PlayerInteract.Instance.OnGoal += PlayerInteract_OnGoal;
        PlayerInteract.Instance.OnDead += PlayerInteract_OnDead;
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
        Show();
    }

    private void PlayerInteract_OnDead(object sender, EventArgs e)
    {
        titleTextMesh.text = "YOU ARE DEAD";
        playButtonTextMesh.text = "WAKE UP";
        playButtonClickAction = GameManager.Instance.RetryLevel;
        Show();
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
