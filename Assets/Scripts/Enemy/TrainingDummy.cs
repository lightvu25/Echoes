using UnityEngine;

public class TrainingDummy : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100000;
    [SerializeField] private float regenDelay = 3f;
    [SerializeField] private int regenRate = 10000;

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
}
