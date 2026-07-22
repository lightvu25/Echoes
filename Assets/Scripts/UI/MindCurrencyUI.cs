using UnityEngine;
using TMPro;

public class MindCurrencyUI : MonoBehaviour
{
    [Header("Currency Text Elements")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI shardsText;

    private void Update()
    {
        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            if (goldText != null)
            {
                goldText.text = GameSession.Instance.currentRun.runGold.ToString();
            }

            if (shardsText != null)
            {
                shardsText.text = GameSession.Instance.currentRun.currentAstralShards.ToString();
            }
        }
    }
}
