using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsTextMesh;
    [SerializeField] private TextMeshProUGUI levelTextMesh;
    [SerializeField] private Image barImage;

    [Header("Currencies")]
    [SerializeField] private TextMeshProUGUI coinsTextMesh;
    [SerializeField] private TextMeshProUGUI memsTextMesh;

    private void Start()
    {
        // Subscribe to events
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnGoldChanged += UpdateCoins;
            PlayerStats.Instance.OnMemoryFragmentsChanged += UpdateMems;
            
            // Initial UI update
            UpdateCoins(PlayerStats.Instance.CurrentGold);
            UpdateMems(PlayerStats.Instance.MemoryFragments);
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnGoldChanged -= UpdateCoins;
            PlayerStats.Instance.OnMemoryFragmentsChanged -= UpdateMems;
        }
    }

    private void UpdateCoins(int amount)
    {
        if (coinsTextMesh != null)
            coinsTextMesh.text = amount.ToString();
    }

    private void UpdateMems(int amount)
    {
        if (memsTextMesh != null)
            memsTextMesh.text = amount.ToString();
    }

    private void Update()
    {
        UpdateStatsTextMesh();
    }

    private void UpdateStatsTextMesh()
    {
        if (GameManager.Instance != null)
        {
            statsTextMesh.text = GameManager.Instance.GetScore() + "\n" + Mathf.Round(GameManager.Instance.GetTime());
            levelTextMesh.text = GameManager.Instance.GetLevelNumber().ToString();
        }
        
        if (PlayerInteract.Instance != null)
        {
            barImage.fillAmount = PlayerInteract.Instance.GetTimeNormalized();
        }
    }

    /* 
    ========================================================================
    ARCHITECTURAL NOTE: SKILLS UI (Upcoming Feature)
    ========================================================================
    When implementing the Skills UI, do NOT use Update() to poll for cooldowns.
    Create a new UI Controller (e.g., `SkillUIController`) or extend this script 
    by adopting an Event-Driven architecture:

    1. Subscribing to Skill Events:
        PlayerSkills.Instance.OnSkillCooldownChanged += UpdateSkillCooldownOverlay;
        PlayerSkills.Instance.OnSkillUnlocked += ShowSkillIcon;

    2. Managing Cooldown Overlays:
        private void UpdateSkillCooldownOverlay(float currentCooldown, float maxCooldown)
        {
            skillCooldownImage.fillAmount = currentCooldown / maxCooldown;
        }

    3. Unsubscribing:
        Always unsubscribe in OnDestroy() to prevent memory leaks.
    ======================================================================== 
    */
}