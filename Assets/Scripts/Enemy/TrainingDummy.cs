using UnityEngine;

public class TrainingDummy : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100000;
    [SerializeField] private float regenDelay = 3f;
    [SerializeField] private int regenRate = 10000;

    [Header("UI")]
    [SerializeField] private Transform pfDamagePopup;

    private HealthSystem healthSystem;
    private float lastDamageTime;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogError("TrainingDummy: HealthSystem component is missing!");
        }
    }

    private void Start()
    {
        if (healthSystem != null)
        {
            // Initialize with massive health
            healthSystem.SetMaxHP(maxHealth, true);
            
            // Subscribe to damage event to track combat state
            healthSystem.OnDamaged += HealthSystem_OnDamaged;
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDamaged -= HealthSystem_OnDamaged;
        }
    }

    private void HealthSystem_OnDamaged(object sender, HealthSystem.DamageEventArgs e)
    {
        // Reset the out-of-combat timer whenever we take damage
        lastDamageTime = Time.time;

        // Spawn Damage Popup
        if (pfDamagePopup != null)
        {
            Transform damagePopupTransform = Instantiate(pfDamagePopup, transform.position + Vector3.up * 1f, Quaternion.identity);
            // Use GetComponentInChildren since DamagePopup may be on a child object
            DamagePopup damagePopup = damagePopupTransform.GetComponentInChildren<DamagePopup>();
            if (damagePopup != null)
            {
                damagePopup.Setup(e.damageAmount);
            }
        }
    }

    private void Update()
    {
        if (healthSystem == null || healthSystem.IsDead) return;

        // Check if we haven't taken damage for 'regenDelay' seconds
        if (Time.time >= lastDamageTime + regenDelay)
        {
            // If strictly below MaxHP, heal
            if (healthSystem.CurrentHP < healthSystem.MaxHP)
            {
                healthSystem.Heal(regenRate);
            }
        }
    }
    // IDamageable Implementation
    public bool IsDead => healthSystem != null && healthSystem.IsDead;
    public Transform Transform => transform;
    public float Defense => healthSystem != null ? healthSystem.Defense : 0f;

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damageInfo);
        }
    }
}
