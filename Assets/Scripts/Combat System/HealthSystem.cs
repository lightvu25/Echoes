using System;
using UnityEngine;

/// <summary>
/// Manages HP, damage reception, i-frames, and death.
/// Attach to any entity that can take damage.
/// </summary>
public class HealthSystem : MonoBehaviour
{
    // ===== Events =====
    public event EventHandler<DamageEventArgs> OnDamaged;
    public event EventHandler<HealEventArgs> OnHealed;
    public event EventHandler OnDeath;

    public class DamageEventArgs : EventArgs
    {
        public int damageAmount;
        public int currentHP;
        public int maxHP;
        public DamageInfo damageInfo;
    }

    public class HealEventArgs : EventArgs
    {
        public int healAmount;
        public int currentHP;
        public int maxHP;
    }

    // ===== Stats =====
    [Header("Health")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP;

    [Header("Defense")]
    [SerializeField] private float defense = 0f;

    [Header("Invincibility Frames")]
    [SerializeField] private float iFrameDuration = 0.5f;
    [SerializeField] private bool hasIFrames = true;

    // ===== State =====
    private bool isInvincible = false;
    private float iFrameTimer = 0f;
    private bool isDead = false;

    // ===== References =====
    private SpriteColorFlasher colorFlasher;
    private TimeFreezer timeFreezer;

    // ===== Properties =====
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public float Defense => defense;
    public bool IsDead => isDead;
    public bool IsInvincible => isInvincible;
    public float HPPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;

    private void Awake()
    {
        currentHP = maxHP;
        colorFlasher = GetComponent<SpriteColorFlasher>();
        timeFreezer = GetComponent<TimeFreezer>();
    }

    private void Update()
    {
        // Handle i-frame timer
        if (isInvincible && hasIFrames)
        {
            iFrameTimer -= Time.deltaTime;
            if (iFrameTimer <= 0f)
            {
                isInvincible = false;
            }
        }
    }

    /// <summary>
    /// Apply damage using the two-stage pipeline.
    /// </summary>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || isInvincible) return;

        // Calculate final damage
        int finalDamage = DamageCalculator.CalculateFinalDamage(damageInfo, defense);

        // Apply damage
        currentHP -= finalDamage;

        // Trigger i-frames
        if (hasIFrames)
        {
            isInvincible = true;
            iFrameTimer = iFrameDuration;
        }

        // Visual feedback
        if (colorFlasher != null)
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                colorFlasher.FlashColor(spriteRenderer, 0.1f, Color.white);
            }
        }

        // Hit freeze
        if (timeFreezer != null && damageInfo.hitFreezeTime > 0f)
        {
            timeFreezer.FreezeTime(damageInfo.hitFreezeTime);
        }

        // Fire event
        OnDamaged?.Invoke(this, new DamageEventArgs
        {
            damageAmount = finalDamage,
            currentHP = currentHP,
            maxHP = maxHP,
            damageInfo = damageInfo
        });

        // Check death
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    /// <summary>
    /// Heal the entity.
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;

        int previousHP = currentHP;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        int actualHeal = currentHP - previousHP;

        if (actualHeal > 0)
        {
            OnHealed?.Invoke(this, new HealEventArgs
            {
                healAmount = actualHeal,
                currentHP = currentHP,
                maxHP = maxHP
            });
        }
    }

    /// <summary>
    /// Set max HP (e.g., from CombatStats).
    /// </summary>
    public void SetMaxHP(int newMaxHP, bool healToFull = false)
    {
        maxHP = newMaxHP;
        if (healToFull)
        {
            currentHP = maxHP;
        }
        else
        {
            currentHP = Mathf.Min(currentHP, maxHP);
        }
    }

    /// <summary>
    /// Set defense stat.
    /// </summary>
    public void SetDefense(float newDefense)
    {
        defense = Mathf.Max(0f, newDefense);
    }

    /// <summary>
    /// Force invincibility (e.g., during dash).
    /// </summary>
    public void SetInvincible(bool invincible, float duration = 0f)
    {
        isInvincible = invincible;
        if (invincible && duration > 0f)
        {
            iFrameTimer = duration;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Revive the entity with specified HP.
    /// </summary>
    public void Revive(int hp = -1)
    {
        isDead = false;
        isInvincible = false;
        currentHP = hp > 0 ? Mathf.Min(hp, maxHP) : maxHP;
    }
}
