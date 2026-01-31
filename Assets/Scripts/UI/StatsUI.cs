using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsTextMesh;
    [SerializeField] private TextMeshProUGUI levelTextMesh;
    [SerializeField] private Image barImage;

    private void Update()
    {
        UpdateStatsTextMesh();
    }

    private void UpdateStatsTextMesh()
    {
        statsTextMesh.text = GameManager.Instance.GetScore() + "\n" + Mathf.Round(GameManager.Instance.GetTime());
        barImage.fillAmount = PlayerInteract.Instance.GetTimeNormalized();
        levelTextMesh.text = GameManager.Instance.GetLevelNumber().ToString();
    }
}
