using UnityEngine;

public class HealingFlask : MonoBehaviour
{
    [Header("Flask Settings")]
    [SerializeField] private int baseUses = 2;
    [SerializeField] private string mindGardenUpgradeID = "HealingFlask_Upgrade";
    [SerializeField] private int extraUsesPerUpgrade = 1;
    
    [Header("Heal Settings")]
    [Tooltip("Percentage of HP to heal (0.4 = 40%)")]
    [SerializeField] private float healPercentage = 0.4f;
    
    [Tooltip("If true, heals 40% of CURRENT HP (as requested). If false, heals 40% of MAX HP.")]
    [SerializeField] private bool scaleWithCurrentHP = true; 

    private int currentUses;
    private HealthSystem playerHealth;

    private void Start()
    {
        playerHealth = GetComponent<HealthSystem>();
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<HealthSystem>();
        }

        Replenish();
    }

    /// <summary>
    /// Calculates the max uses by adding the base uses and checking the Mind Garden progression.
    /// </summary>
    public int GetMaxUses()
    {
        int maxUses = baseUses;

        // Check if the mind garden upgrade is unlocked in the current profile
        if (GameSession.Instance != null && GameSession.Instance.currentProfile != null)
        {
            if (GameSession.Instance.currentProfile.HasSkill(mindGardenUpgradeID))
            {
                maxUses += extraUsesPerUpgrade;
            }
        }

        return maxUses;
    }

    /// <summary>
    /// Refills the flask to its maximum uses. Call this at checkpoints or respawn.
    /// </summary>
    public void Replenish()
    {
        currentUses = GetMaxUses();
    }

    /// <summary>
    /// Uses the flask to heal the player.
    /// </summary>
    public void Use()
    {
        if (currentUses <= 0)
        {
            Debug.Log("[HealingFlask] No uses remaining.");
            return;
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("[HealingFlask] No HealthSystem found.");
            return;
        }

        if (playerHealth.CurrentHP >= playerHealth.MaxHP)
        {
            Debug.Log("[HealingFlask] Player is already at full health.");
            return;
        }

        currentUses--;

        // Calculate heal amount
        int baseHpForCalculation = scaleWithCurrentHP ? playerHealth.CurrentHP : playerHealth.MaxHP;
        int healAmount = Mathf.RoundToInt(baseHpForCalculation * healPercentage);

        // Ensure we always heal at least 1 HP if used, to avoid a 0 heal when HP is extremely low.
        if (healAmount < 1) healAmount = 1;

        playerHealth.Heal(healAmount);
        Debug.Log($"[HealingFlask] Used flask. Healed {healAmount} HP. Uses remaining: {currentUses}/{GetMaxUses()}");
    }

    public int GetCurrentUses() => currentUses;
}
