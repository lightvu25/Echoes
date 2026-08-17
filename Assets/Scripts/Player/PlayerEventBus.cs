using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central hub for player-related events.
/// Relics and other systems subscribe here instead of tightly coupling to specific components.
/// Uses auto-bridging to hook into core systems without modifying them.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
[DisallowMultipleComponent]
public class PlayerEventBus : MonoBehaviour
{
    public static PlayerEventBus Instance { get; private set; }

    // --- Core Systems to Bridge ---
    private HealthSystem healthSystem;
    private PlayerAttack playerAttack;
    private AttackHitbox currentHitbox;
    private Room currentRoom;

    // --- Events ---

    /// <summary>
    /// Fired before the player takes damage. Allows modifying the damage amount via ref.
    /// </summary>
    public delegate void DamageModifierHandler(ref int damageAmount, ref DamageInfo info);
    public event DamageModifierHandler OnBeforeDamageTaken;
    private readonly List<PrioritizedDamageModifier> prioritizedDamageModifiers = new List<PrioritizedDamageModifier>();

    private readonly struct PrioritizedDamageModifier
    {
        public PrioritizedDamageModifier(DamageModifierHandler handler, int priority)
        {
            Handler = handler;
            Priority = priority;
        }
        public DamageModifierHandler Handler { get; }
        public int Priority { get; }
    }

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
    public event Action<EnemyKillEvent> OnEnemyKilledDetailed;

    /// <summary>
    /// Fired when a combat room is cleared. (Must be called externally by level logic).
    /// </summary>
    public event Action OnRoomCleared;
    public event Action<Room> OnRoomEntered;
    public event Action OnPickupCollected;
    public event Action<AttackHitbox.HitEventArgs> OnSuccessfulHit;
    public event Action<PlayerAttack.PlungeImpactEventArgs> OnPlungeImpact;

    public readonly struct EnemyKillEvent
    {
        public EnemyKillEvent(EnemyCombat enemy, DamageInfo killingBlow)
        {
            Enemy = enemy;
            KillingBlow = killingBlow;
        }

        public EnemyCombat Enemy { get; }
        public DamageInfo KillingBlow { get; }
        public bool IsEliteOrBoss => Enemy != null && Enemy.IsEliteOrBoss;
    }

    // --- Lifecycle ---

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(this);
            return;
        }

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
            currentHitbox = playerAttack.CurrentAttackHitbox;
            if (currentHitbox != null)
            {
                currentHitbox.OnBeforeDamageApplied += HandleBeforeOutgoingDamage;
                currentHitbox.OnHitTarget += HandleSuccessfulHit;
            }
            playerAttack.OnPlungeImpact += HandlePlungeImpact;
        }

        EnemyCombat.OnAnyEnemyDied += HandleEnemyDied;
        Room.OnRoomEntered += HandleRoomEntered;
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
            playerAttack.OnPlungeImpact -= HandlePlungeImpact;
        }

        UnsubscribeFromCurrentHitbox();
        EnemyCombat.OnAnyEnemyDied -= HandleEnemyDied;
        Room.OnRoomEntered -= HandleRoomEntered;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // --- Bridge Implementations ---

    private void HandleBeforeTakeDamage(ref int damageAmount, ref DamageInfo info)
    {
        // Legacy modifiers run first. Generated Relics use deterministic phase priorities.
        OnBeforeDamageTaken?.Invoke(ref damageAmount, ref info);
        foreach (PrioritizedDamageModifier modifier in prioritizedDamageModifiers)
            modifier.Handler(ref damageAmount, ref info);

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

    public void RegisterDamageModifier(DamageModifierHandler handler, int priority)
    {
        if (handler == null) return;
        UnregisterDamageModifier(handler);
        prioritizedDamageModifiers.Add(new PrioritizedDamageModifier(handler, priority));
        prioritizedDamageModifiers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    public void UnregisterDamageModifier(DamageModifierHandler handler)
    {
        if (handler == null) return;
        prioritizedDamageModifiers.RemoveAll(entry => entry.Handler == handler);
    }

    private void HandleHealed(object sender, HealthSystem.HealEventArgs e)
    {
        OnHealed?.Invoke(e.healAmount);
    }

    private void UnsubscribeFromCurrentHitbox()
    {
        if (currentHitbox != null)
        {
            currentHitbox.OnBeforeDamageApplied -= HandleBeforeOutgoingDamage;
            currentHitbox.OnHitTarget -= HandleSuccessfulHit;
            currentHitbox = null;
        }
    }

    private void HandleBeforeOutgoingDamage(IDamageable target, ref DamageInfo info)
    {
        OnBeforeOutgoingDamage?.Invoke(target, ref info);
    }

    private void HandleSuccessfulHit(object sender, AttackHitbox.HitEventArgs e) => OnSuccessfulHit?.Invoke(e);

    public void FireSuccessfulHit(IDamageable target, DamageInfo info, int finalDamage)
    {
        if (target == null) return;
        OnSuccessfulHit?.Invoke(new AttackHitbox.HitEventArgs
        {
            target = target,
            damageInfo = info,
            finalDamage = finalDamage
        });
    }

    private void HandleEnemyDied(EnemyCombat enemy, DamageInfo killingBlow)
    {
        if (killingBlow.attacker == null || killingBlow.attacker.transform.root != transform.root) return;
        OnEnemyKilled?.Invoke();
        OnEnemyKilledDetailed?.Invoke(new EnemyKillEvent(enemy, killingBlow));
    }

    private void HandleRoomEntered(Room room)
    {
        if (room == null || room == currentRoom) return;
        currentRoom = room;
        OnRoomEntered?.Invoke(room);
    }
    private void HandlePlungeImpact(PlayerAttack.PlungeImpactEventArgs args) => OnPlungeImpact?.Invoke(args);

    public void ApplyOutgoingModifiers(IDamageable target, ref DamageInfo info)
    {
        OnBeforeOutgoingDamage?.Invoke(target, ref info);
    }

    // --- External Triggers ---

    public void FireEnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }

    public void FirePickupCollected() => OnPickupCollected?.Invoke();

    public void FireRoomCleared()
    {
        OnRoomCleared?.Invoke();
    }
}
