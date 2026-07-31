using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    // ===== Events =====
    public event EventHandler<DamageEventArgs> OnDamaged;
    public event EventHandler<HealEventArgs> OnHealed;
    public event EventHandler OnDeath;
    public event Action<int> OnSlotsChanged;
    public event Action OnMaxHPGained;
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
    [Header("Data (Optional)")]
    [Tooltip("If assigned, overrides maxHP, defense, and iFrameDuration below.")]
    [SerializeField] private CombatStats combatStatsAsset;

    [Header("Health")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP;
    // maxSlots is now dynamically calculated based on maxHP / 100

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
    public CombatStats CombatStats => combatStatsAsset;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int MaxSlots => Mathf.Max(1, maxHP / 100);
    public int UnlockedSlots 
    {
        get 
        {
            if (MaxSlots <= 0 || maxHP <= 0) return 0;
            float hpPerSlot = (float)maxHP / MaxSlots;
            return Mathf.CeilToInt(currentHP / hpPerSlot);
        }
    }
    public float Defense => defense;
    public bool IsDead => isDead;
    public bool IsInvincible => isInvincible;
    public float HPPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public delegate void PreDamageHandler(ref int damageAmount, ref DamageInfo info);
    public event PreDamageHandler OnBeforeTakeDamage;

    private void Awake()
    {
        if (combatStatsAsset != null)
        {
            maxHP = combatStatsAsset.maxHP;
            defense = combatStatsAsset.defense;
            iFrameDuration = combatStatsAsset.iFrameDuration;
        }

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

    public int TakeDamage(DamageInfo damageInfo)
    {
        if (isDead || isInvincible) return 0;

        // Play Impact Audio from Attacker
        if (damageInfo.attacker != null)
        {
            var audioManager = damageInfo.attacker.GetComponentInChildren<EntityAudioManager>();
            if (audioManager != null)
            {
                string soundId = damageInfo.isCritical ? "Attack Critical" : "Attack Normal";
                audioManager.PlaySound(soundId);
            }
        }

        int finalDamage = DamageCalculator.CalculateFinalDamage(damageInfo, defense);

        int previousSlots = UnlockedSlots;

        OnBeforeTakeDamage?.Invoke(ref finalDamage, ref damageInfo);

        currentHP -= finalDamage;

        if (UnlockedSlots != previousSlots)
        {
            OnSlotsChanged?.Invoke(UnlockedSlots);
            if (UnlockedSlots < previousSlots) 
            {
                OnUnlockedSlotsDecreased?.Invoke(UnlockedSlots);
            }
        }

        if (damagePopupPrefab != null && finalDamage > 0)
        {
            GameObject popupObj = ObjectPoolManager.SpawnObject(damagePopupPrefab, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.UI);
            DamagePopup damagePopup = popupObj.GetComponentInChildren<DamagePopup>();
            if (damagePopup != null)
            {
                damagePopup.Setup(finalDamage, damageInfo.isCritical, damageInfo.isTrueDamage);
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

        return finalDamage;
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        int previousHP = currentHP;
        int previousSlots = UnlockedSlots;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        int actualHeal = currentHP - previousHP;

        if (UnlockedSlots != previousSlots)
        {
            OnSlotsChanged?.Invoke(UnlockedSlots);
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
        int previousMaxHP = maxHP; // Thêm dòng này

        maxHP = newMaxHP;
        if (healToFull) currentHP = maxHP;
        else currentHP = Mathf.Min(currentHP, maxHP);

        // Gọi event nếu Max HP tăng lên
        if (maxHP > previousMaxHP) OnMaxHPGained?.Invoke();

        if (UnlockedSlots != previousSlots) OnSlotsChanged?.Invoke(UnlockedSlots);
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
