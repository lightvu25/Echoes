using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    // ===== Events =====
    public event EventHandler<DamageEventArgs> OnDamaged;
    public event EventHandler<HealEventArgs> OnHealed;
    public event EventHandler OnDeath;
    public event Action<int> OnSlotsChanged;
    public event Action<int> OnUnlockedSlotsDecreased;

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
    [SerializeField] private int maxSlots = 3;

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
    [Header("UI")]
    [SerializeField] private GameObject damagePopupPrefab;

    private SpriteColorFlasher colorFlasher;
    private TimeFreezer timeFreezer;

    // ===== Properties =====
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int MaxSlots => maxSlots;
    public int UnlockedSlots 
    {
        get 
        {
            if (maxSlots <= 0 || maxHP <= 0) return 0;
            float hpPerSlot = (float)maxHP / maxSlots;
            return Mathf.CeilToInt(currentHP / hpPerSlot);
        }
    }
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

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || isInvincible) return;

        int finalDamage = DamageCalculator.CalculateFinalDamage(damageInfo, defense);

        int previousSlots = UnlockedSlots;
        currentHP -= finalDamage;

        int newSlots = UnlockedSlots;
        if (newSlots != previousSlots)
        {
            OnSlotsChanged?.Invoke(newSlots);
            if (newSlots < previousSlots)
            {
                OnUnlockedSlotsDecreased?.Invoke(newSlots);
            }
        }

        if (damagePopupPrefab != null && finalDamage > 0)
        {
            GameObject popupObj = ObjectPoolManager.SpawnObject(damagePopupPrefab, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.UI);
            DamagePopup damagePopup = popupObj.GetComponentInChildren<DamagePopup>();
            if (damagePopup != null)
            {
                damagePopup.Setup(finalDamage);
            }
        }

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

    public void Heal(int amount)
    {
        if (isDead) return;

        int previousHP = currentHP;
        int previousSlots = UnlockedSlots;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        int actualHeal = currentHP - previousHP;

        int newSlots = UnlockedSlots;
        if (newSlots != previousSlots)
        {
            OnSlotsChanged?.Invoke(newSlots);
            if (newSlots < previousSlots)
            {
                OnUnlockedSlotsDecreased?.Invoke(newSlots);
            }
        }

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

    public void SetMaxHP(int newMaxHP, bool healToFull = false)
    {
        int previousSlots = UnlockedSlots;
        maxHP = newMaxHP;
        if (healToFull)
        {
            currentHP = maxHP;
        }
        else
        {
            currentHP = Mathf.Min(currentHP, maxHP);
        }

        int newSlots = UnlockedSlots;
        if (newSlots != previousSlots)
        {
            OnSlotsChanged?.Invoke(newSlots);
            if (newSlots < previousSlots)
            {
                OnUnlockedSlotsDecreased?.Invoke(newSlots);
            }
        }
    }

    public void SetDefense(float newDefense)
    {
        defense = Mathf.Max(0f, newDefense);
    }

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

    public void Revive(int hp = -1)
    {
        isDead = false;
        isInvincible = false;
        currentHP = hp > 0 ? Mathf.Min(hp, maxHP) : maxHP;
    }
}
