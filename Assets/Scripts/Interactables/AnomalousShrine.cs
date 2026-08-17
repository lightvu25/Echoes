using UnityEngine;

public class AnomalousShrine : MonoBehaviour, IInteractable, IDamageable
{
    [Header("Rewards")]
    public EchoData curseEchoReward;

    [Header("VFX")]
    public GameObject shatterVFX;

    private bool isUsed = false;

    private void Awake()
    {
        InteractableTrigger trigger = GetComponent<InteractableTrigger>();
        if (trigger != null)
        {
            trigger.maxInteractions = 1;
        }
    }

    // =============================================
    // IDamageable implementation
    // =============================================
    public bool IsDead => isUsed;
    public Transform Transform => transform;
    public float Defense => 0f;

    // =============================================
    // ABSORB — Safe route (player interacts)
    // =============================================
    public void Interact()
    {
        Debug.Log("[DEBUG] AnomalousShrine.Interact() called! isUsed=" + isUsed);
        if (isUsed) return;
        isUsed = true;

        if (UIManager.Instance != null)
        {
            Debug.Log("[DEBUG] UIManager exists, opening ShrineBlessing panel.");
            UIManager.Instance.OpenPanel(UIPanelType.ShrineBlessing);
        }
        else
        {
            Debug.LogError("[DEBUG] UIManager.Instance is null!");
        }
    }

    // =============================================
    // SHATTER — Sin route (player attacks)
    // =============================================
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isUsed) return;
        isUsed = true;

        RunData run = GameSession.Instance?.currentRun;
        if (run == null) return;

        // Stat rewards
        run.bonusSorcery    += 5;
        run.bonusResonance  += 5;
        run.bonusVitality   += 20;

        // Apply increased Max HP to the live player immediately
        if (PlayerStats.Instance != null)
        {
            HealthSystem hs = PlayerStats.Instance.GetComponent<HealthSystem>();
            if (hs != null)
                hs.ModifyMaxHP(20);
        }

        // Curse
        run.currentLevelBurdens.Add("SHATTER_BURDEN");

        // Loot drop at shrine position
        if (curseEchoReward != null)
            Instantiate(curseEchoReward, transform.position, Quaternion.identity);

        // VFX
        if (shatterVFX != null)
            Instantiate(shatterVFX, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
