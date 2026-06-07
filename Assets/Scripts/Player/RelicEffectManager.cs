using UnityEngine;

[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(InventoryManager))]
public class RelicEffectManager : MonoBehaviour
{
    private PlayerAttack playerAttack;
    private InventoryManager inventoryManager;
    private PlayerCombat playerCombat;

    [Header("Dying Amulet")]
    [SerializeField] private GameObject goldPrefab;

    private void Awake()
    {
        playerAttack = GetComponent<PlayerAttack>();
        inventoryManager = GetComponent<InventoryManager>();
        playerCombat = GetComponent<PlayerCombat>();
    }

    private void OnEnable()
    {
        playerAttack.OnAttackStarted += PlayerAttack_OnAttackStarted;
        playerAttack.OnAttackEnded += PlayerAttack_OnAttackEnded;
    }

    private void Start()
    {
        if (playerAttack.CurrentAttackHitbox != null)
        {
            playerAttack.CurrentAttackHitbox.OnBeforeDamageApplied += AttackHitbox_OnBeforeDamageApplied;
        }
    }

    private void OnDisable()
    {
        playerAttack.OnAttackStarted -= PlayerAttack_OnAttackStarted;
        playerAttack.OnAttackEnded -= PlayerAttack_OnAttackEnded;

        if (playerAttack.CurrentAttackHitbox != null)
        {
            playerAttack.CurrentAttackHitbox.OnBeforeDamageApplied -= AttackHitbox_OnBeforeDamageApplied;
        }
    }

    private void PlayerAttack_OnAttackStarted(object sender, PlayerAttack.AttackEventArgs e)
    {
        if (inventoryManager.HasRelic("Iron_Ring") && e.attackType == PlayerAttack.AttackType.Basic && e.comboStep == 2)
        {
            playerAttack.temporaryDamageMultiplier = 2f;
        }
    }

    private void PlayerAttack_OnAttackEnded(object sender, PlayerAttack.AttackEventArgs e)
    {
        playerAttack.temporaryDamageMultiplier = 1f;
    }

    private void AttackHitbox_OnBeforeDamageApplied(IDamageable target, ref DamageInfo damageInfo)
    {
        if (inventoryManager.HasRelic("Compensating_Saw") && playerAttack.CurrentAttackType == PlayerAttack.AttackType.Heavy)
        {
            if (target is EnemyCombat enemy && enemy.IsKnockedBack)
            {
                damageInfo.multiplicativeStack *= 3f;
            }
        }

        // --- Dying_Amulet Relic ---
        if (inventoryManager.HasRelic("Dying_Amulet") && playerCombat != null && playerCombat.CurrentHP == 1)
        {
            if (goldPrefab != null && target.Transform != null)
            {
                GameObject gold = ObjectPoolManager.SpawnObject(goldPrefab, target.Transform.position, Quaternion.identity, ObjectPoolManager.PoolType.Loot);
                
                if (gold.TryGetComponent(out Collectible collectible))
                {
                    float randomX = UnityEngine.Random.Range(-3f, 3f); 
                    float randomY = UnityEngine.Random.Range(4f, 9f); 
                    collectible.Initialize(1, new Vector2(randomX, randomY));
                }
                else if (gold.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    float randomX = UnityEngine.Random.Range(-3f, 3f);
                    float randomY = UnityEngine.Random.Range(4f, 9f);
                    rb.AddForce(new Vector2(randomX, randomY), ForceMode2D.Impulse);
                }
            }
        }
    }

    public void RefreshEffects()
    {
        // For event-driven relics, HasRelic() checks dynamically, so no action needed here.
        // If stat-modifiers like MaxHP are added, recalculate them here to handle fusions/deletions.
    }
}
