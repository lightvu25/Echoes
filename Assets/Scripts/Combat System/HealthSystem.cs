using System;
using System.Collections.Generic;
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
    private int rawMaxHP;
    private readonly HashSet<object> healingBlockers = new HashSet<object>();
    private readonly HashSet<object> iFrameDisablers = new HashSet<object>();
    private readonly HashSet<object> invincibilitySources = new HashSet<object>();
    private readonly Dictionary<object, float> iFrameDurationBonuses = new Dictionary<object, float>();
    private readonly Dictionary<object, int> maxHpAdditives = new Dictionary<object, int>();
    private readonly Dictionary<object, int> maxHpCaps = new Dictionary<object, int>();
    private bool hasRestoredRunHealth;

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
    public bool IsInvincible => invincibilitySources.Count > 0 ||
                                (isInvincible && iFrameDisablers.Count == 0);
    public float HPPercent => maxHP > 0 ? (float)currentHP / maxHP : 0f;
    public bool LastHitReactionSuppressed { get; private set; }
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
        rawMaxHP = maxHP;
        colorFlasher = GetComponent<SpriteColorFlasher>();
        timeFreezer = GetComponent<TimeFreezer>();
        RestorePlayerRunHealth();
    }

    private void Start()
    {
        if (!hasRestoredRunHealth)
            RestorePlayerRunHealth();
    }

    private void Update()
    {
        PruneDestroyedSources(healingBlockers);
        PruneDestroyedSources(iFrameDisablers);
        PruneDestroyedSources(invincibilitySources);
        PruneDestroyedSources(iFrameDurationBonuses);
        bool maxHpSourcesChanged = PruneDestroyedSources(maxHpAdditives);
        maxHpSourcesChanged |= PruneDestroyedSources(maxHpCaps);
        if (maxHpSourcesChanged) RecalculateMaxHP(false, false);

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
        LastHitReactionSuppressed = false;
        if (isDead || invincibilitySources.Count > 0 ||
            (isInvincible && iFrameDisablers.Count == 0 && !damageInfo.BypassesInvincibilityFrames)) return 0;

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
        LastHitReactionSuppressed = damageInfo.suppressHitReaction;

        // A fully-negated hit must not consume i-frames or emit visual/combat
        // feedback as though damage was accepted.
        if (finalDamage <= 0) return 0;

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
        if (hasIFrames && iFrameDisablers.Count == 0)
        {
            isInvincible = true;
            float bonusDuration = 0f;
            foreach (float bonus in iFrameDurationBonuses.Values) bonusDuration += bonus;
            iFrameTimer = iFrameDuration + bonusDuration;
        }

        // Visual feedback
        if (colorFlasher != null)
        {
            colorFlasher.FlashColor(0.1f, Color.white);
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
        bool died = currentHP <= 0;
        if (died)
        {
            currentHP = 0;
        }

        CapturePlayerRunHealth();
        if (died) Die();

        return finalDamage;
    }

    public void Heal(int amount)
    {
        if (isDead || healingBlockers.Count > 0) return;

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
            CapturePlayerRunHealth();
        }
    }

    public void SetHealingBlocked(object source, bool blocked)
    {
        if (source == null) return;
        if (blocked) healingBlockers.Add(source);
        else healingBlockers.Remove(source);
    }

    public void SetIFramesDisabled(object source, bool disabled)
    {
        if (source == null) return;
        if (disabled) iFrameDisablers.Add(source);
        else iFrameDisablers.Remove(source);
        if (iFrameDisablers.Count > 0) isInvincible = false;
    }

    public void SetIFrameDurationBonus(object source, float seconds)
    {
        if (source == null) return;
        if (seconds <= 0f) iFrameDurationBonuses.Remove(source);
        else iFrameDurationBonuses[source] = seconds;
    }

    public void SetMaxHP(int newMaxHP, bool healToFull = false)
    {
        int requested = Mathf.Max(1, newMaxHP);
        int additiveTotal = 0;
        foreach (int additive in maxHpAdditives.Values) additiveTotal += additive;
        rawMaxHP = Mathf.Max(1, requested - additiveTotal);
        RecalculateMaxHP(healToFull, false);
    }

    public void ModifyMaxHP(int delta, bool healAddedAmount = false, bool notifyProgressionGain = true)
    {
        if (delta == 0) return;
        rawMaxHP = Mathf.Max(1, rawMaxHP + delta);
        RecalculateMaxHP(false, notifyProgressionGain && delta > 0);
        if (healAddedAmount && delta > 0) Heal(delta);
    }

    public void SetMaxHPModifier(object source, int additiveHP, bool healAddedAmount = false)
    {
        if (source == null) return;
        int previousAdditive = maxHpAdditives.TryGetValue(source, out int value) ? value : 0;
        if (additiveHP == 0) maxHpAdditives.Remove(source);
        else maxHpAdditives[source] = additiveHP;
        RecalculateMaxHP(false, false);
        if (healAddedAmount && additiveHP > previousAdditive) Heal(additiveHP - previousAdditive);
    }

    public void SetMaxHPCap(object source, int cap)
    {
        if (source == null) return;
        if (cap <= 0) maxHpCaps.Remove(source);
        else maxHpCaps[source] = Mathf.Max(1, cap);
        RecalculateMaxHP(false, false);
    }

    private void RecalculateMaxHP(bool healToFull, bool notifyProgressionGain)
    {
        int previousSlots = UnlockedSlots;
        int previousMaxHP = maxHP;
        int calculated = rawMaxHP;
        foreach (int additive in maxHpAdditives.Values) calculated += additive;
        foreach (int cap in maxHpCaps.Values) calculated = Mathf.Min(calculated, cap);
        maxHP = Mathf.Max(1, calculated);

        if (healToFull) currentHP = maxHP;
        else currentHP = Mathf.Min(currentHP, maxHP);

        if (notifyProgressionGain && maxHP > previousMaxHP) OnMaxHPGained?.Invoke();
        if (UnlockedSlots != previousSlots) OnSlotsChanged?.Invoke(UnlockedSlots);
        CapturePlayerRunHealth();
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

    public void SetInvincible(object source, bool invincible)
    {
        if (source == null) return;
        if (invincible) invincibilitySources.Add(source);
        else invincibilitySources.Remove(source);
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
        CapturePlayerRunHealth();
    }

    private void RestorePlayerRunHealth()
    {
        if (!IsPlayerHealthSystem()) return;

        RunData run = GameSession.Instance?.currentRun;
        if (run == null) return;

        if (run.maxHealth <= 0)
        {
            CapturePlayerRunHealth();
            hasRestoredRunHealth = true;
            return;
        }

        rawMaxHP = Mathf.Max(1, run.maxHealth);
        maxHP = rawMaxHP;
        currentHP = Mathf.Clamp(run.currentHealth, 0, maxHP);

        // Restoring zero HP here used to create a half-dead player: combat and
        // enemy AI considered the player dead, but no OnDeath event was emitted
        // to drive the death sequence. A death is resolved in the scene where it
        // occurs; a newly-created player must always enter the scene alive.
        if (currentHP <= 0)
        {
            currentHP = maxHP;
            CapturePlayerRunHealth();
            Debug.LogWarning(
                "[HealthSystem] A player scene instance was given zero saved HP. " +
                "Restored it to full health instead of creating a silent dead state.",
                this);
        }

        isDead = false;
        hasRestoredRunHealth = true;
    }

    private void CapturePlayerRunHealth()
    {
        if (!IsPlayerHealthSystem()) return;

        RunData run = GameSession.Instance?.currentRun;
        if (run == null) return;

        run.maxHealth = maxHP;
        run.currentHealth = Mathf.Clamp(currentHP, 0, maxHP);
    }

    private bool IsPlayerHealthSystem()
    {
        return CompareTag("Player") || GetComponent<PlayerStats>() != null;
    }

    private static void PruneDestroyedSources(HashSet<object> sources)
    {
        sources.RemoveWhere(source => source is UnityEngine.Object unityObject && unityObject == null);
    }

    private static bool PruneDestroyedSources<TValue>(Dictionary<object, TValue> sources)
    {
        List<object> stale = null;
        foreach (object source in sources.Keys)
        {
            if (source is UnityEngine.Object unityObject && unityObject == null)
            {
                stale ??= new List<object>();
                stale.Add(source);
            }
        }
        if (stale == null) return false;
        foreach (object source in stale) sources.Remove(source);
        return true;
    }
}
