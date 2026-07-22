using System;
using UnityEngine;

/// <summary>
/// Central hub for player-related events.
/// Relics and other systems subscribe here instead of tightly coupling to specific components.
/// Uses auto-bridging to hook into core systems without modifying them.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class PlayerEventBus : MonoBehaviour
{
    public static PlayerEventBus Instance { get; private set; }

    // --- Core Systems to Bridge ---
    private HealthSystem healthSystem;
    private PlayerAttack playerAttack;
    private AttackHitbox currentHitbox;

    // --- Events ---

    /// <summary>
    /// Fired before the player takes damage. Allows modifying the damage amount via ref.
    /// </summary>
    public delegate void DamageModifierHandler(ref int damageAmount, ref DamageInfo info);
    public event DamageModifierHandler OnBeforeDamageTaken;

    /// <summary>
    /// Fired when incoming damage would reduce the player's HP to 0 or below.
    /// Relics can set preventDeath to true and zero the damage to override death.
    /// </summary>
    public delegate void FatalDamageHandler(ref bool preventDeath);
    public event FatalDamageHandler OnFatalDamage;

    /// <summary>
    /// Fired when the player is about to apply damage to an enemy.
    /// Relics can modify the outgoing DamageInfo via ref (e.g. force critical hits).
    /// </summary>
    public delegate void OutgoingDamageHandler(IDamageable target, ref DamageInfo info);
    public event OutgoingDamageHandler OnBeforeOutgoingDamage;

    /// <summary>
    /// Fired when the player is healed.
    /// </summary>
    public event Action<int> OnHealed;

    /// <summary>
    /// Fired when an enemy is killed. (Must be called externally by enemy death logic).
    /// </summary>
    public event Action OnEnemyKilled;

    /// <summary>
    /// Fired when a combat room is cleared. (Must be called externally by level logic).
    /// </summary>
    public event Action OnRoomCleared;

    // --- Lifecycle ---

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);

        healthSystem = GetComponent<HealthSystem>();
        playerAttack = GetComponent<PlayerAttack>();
    }

    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnBeforeTakeDamage += HandleBeforeTakeDamage;
            healthSystem.OnHealed += HandleHealed;
        }

        if (playerAttack != null)
        {
            playerAttack.OnAttackStarted += HandleAttackStarted;
            playerAttack.OnAttackEnded += HandleAttackEnded;
            playerAttack.OnAttackCancelled += HandleAttackEnded;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.OnBeforeTakeDamage -= HandleBeforeTakeDamage;
            healthSystem.OnHealed -= HandleHealed;
        }

        if (playerAttack != null)
        {
            playerAttack.OnAttackStarted -= HandleAttackStarted;
            playerAttack.OnAttackEnded -= HandleAttackEnded;
            playerAttack.OnAttackCancelled -= HandleAttackEnded;
        }

        UnsubscribeFromCurrentHitbox();
    }

    // --- Bridge Implementations ---

    private void HandleBeforeTakeDamage(ref int damageAmount, ref DamageInfo info)
    {
        // 1. Allow normal damage modification first
        OnBeforeDamageTaken?.Invoke(ref damageAmount, ref info);

        // 2. Check for fatal damage
        if (damageAmount >= healthSystem.CurrentHP)
        {
            bool preventDeath = false;
            OnFatalDamage?.Invoke(ref preventDeath);

            if (preventDeath)
            {
                damageAmount = 0; // Prevent the damage application
            }
        }
    }

    private void HandleHealed(object sender, HealthSystem.HealEventArgs e)
    {
        OnHealed?.Invoke(e.healAmount);
    }

    private void HandleAttackStarted(object sender, PlayerAttack.AttackEventArgs e)
    {
        UnsubscribeFromCurrentHitbox();
        currentHitbox = playerAttack.CurrentAttackHitbox;
        if (currentHitbox != null)
        {
            currentHitbox.OnBeforeDamageApplied += HandleBeforeOutgoingDamage;
        }
    }

    private void HandleAttackEnded(object sender, PlayerAttack.AttackEventArgs e)
    {
        UnsubscribeFromCurrentHitbox();
    }

    private void UnsubscribeFromCurrentHitbox()
    {
        if (currentHitbox != null)
        {
            currentHitbox.OnBeforeDamageApplied -= HandleBeforeOutgoingDamage;
            currentHitbox = null;
        }
    }

    private void HandleBeforeOutgoingDamage(IDamageable target, ref DamageInfo info)
    {
        OnBeforeOutgoingDamage?.Invoke(target, ref info);
    }

    // --- External Triggers ---

    public void FireEnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }

    public void FireRoomCleared()
    {
        OnRoomCleared?.Invoke();
    }
}
