using UnityEngine;

public class EchoStatusReceiver : MonoBehaviour
{
    public bool IsSlowed { get; private set; }
    public bool IsFrozen { get; private set; }
    public bool IsBurning { get; private set; }
    public GameObject BurnAttacker => burnAttacker;
    public bool IsPoisoned { get; private set; }
    public bool IsSilenced { get; private set; }
    public bool IsStunned { get; private set; }
    public bool IsVoidMarked { get; set; }
    
    public float SpeedMultiplier 
    {
        get 
        {
            if (IsFrozen || IsStunned) return 0f;
            if (IsSlowed) return 0.5f;
            return 1f;
        }
    }

    public Color CurrentTargetColor
    {
        get
        {
            if (IsFrozen) return new Color(0.2f, 0.5f, 1f);
            if (IsSlowed) return new Color(0.5f, 0.8f, 1f);
            return originalColor;
        }
    }
    
    private float slowTimer = 0f;
    private float freezeTimer = 0f;
    private float burnTimer = 0f;
    private float silenceTimer = 0f;
    private float burnTickTimer = 0f;
    private GameObject burnAttacker;
    private float poisonTimer = 0f;
    private float poisonTickTimer = 0f;
    private int poisonDamage = 3;
    private GameObject poisonAttacker;
    private float stunTimer = 0f;

    private SpriteRenderer targetSprite;
    private Color originalColor;
    private Animator targetAnimator;

    [Header("VFX Prefabs")]
    public GameObject iceBlockPrefab;
    private GameObject currentIceBlock;
    private ParticleSystem currentBurnVFX;

    private void Awake()
    {
        targetSprite = GetComponentInChildren<SpriteRenderer>();
        targetAnimator = GetComponentInChildren<Animator>();
        if (targetSprite != null) originalColor = targetSprite.color;
    }

    private void OnDisable()
    {
        IsSlowed = false;
        IsFrozen = false;
        IsBurning = false;
        IsPoisoned = false;
        IsSilenced = false;
        IsStunned = false;
        IsVoidMarked = false;
        poisonAttacker = null;
        burnAttacker = null;
        if (targetSprite != null) targetSprite.color = originalColor;
        if (targetAnimator != null) targetAnimator.speed = 1f;
        if (currentIceBlock != null) Destroy(currentIceBlock);
        if (currentBurnVFX != null) Destroy(currentBurnVFX.gameObject);
    }

    private void Update()
    {
        // Xử lý đếm ngược Làm Chậm
        if (IsSlowed) 
        { 
            slowTimer -= Time.deltaTime; 
            if (slowTimer <= 0) 
            {
                IsSlowed = false;
                if (targetSprite != null && !IsFrozen) targetSprite.color = originalColor;
                if (targetAnimator != null && !IsFrozen) targetAnimator.speed = 1f;
            } 
        }
        
        // Xử lý đếm ngược Đóng Băng
        if (IsFrozen)
        { 
            freezeTimer -= Time.deltaTime; 
            if (freezeTimer <= 0) 
            {
                IsFrozen = false;
                if (targetSprite != null) 
                {
                    targetSprite.color = IsSlowed ? new Color(0.5f, 0.8f, 1f) : originalColor;
                }
                
                if (targetAnimator != null && !IsSlowed) targetAnimator.speed = 1f;
                else if (targetAnimator != null && IsSlowed) targetAnimator.speed = 0.5f;

                if (currentIceBlock != null)
                {
                    Destroy(currentIceBlock);
                }
            } 
        }

        if (IsBurning)
        {
            burnTimer -= Time.deltaTime;
            burnTickTimer -= Time.deltaTime;
            if (burnTickTimer <= 0)
            {
                IDamageable dmg = GetComponent<IDamageable>();
                if (dmg != null) 
                { 
                    DamageInfo tick = DamageInfo.Create(5, burnAttacker != null ? burnAttacker : gameObject);
                    tick.damageSource = DamageSourceType.Burn; 
                    dmg.TakeDamage(tick); 
                }
                burnTickTimer = 0.5f;
            }
            if (burnTimer <= 0)
            {
                IsBurning = false;
                burnAttacker = null;
                if (currentBurnVFX != null) Destroy(currentBurnVFX.gameObject);
            }
        }

        if (IsPoisoned)
        {
            poisonTimer -= Time.deltaTime;
            poisonTickTimer -= Time.deltaTime;
            if (poisonTickTimer <= 0f)
            {
                IDamageable damageable = GetComponent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    DamageInfo tick = DamageInfo.Create(poisonDamage, poisonAttacker != null ? poisonAttacker : gameObject);
                    tick.damageSource = DamageSourceType.Poison;
                    damageable.TakeDamage(tick);
                }
                poisonTickTimer = 0.75f;
            }
            if (poisonTimer <= 0f)
            {
                IsPoisoned = false;
                poisonAttacker = null;
            }
        }

        if (IsSilenced) 
        { 
            silenceTimer -= Time.deltaTime; 
            if (silenceTimer <= 0) IsSilenced = false; 
        }

        if (IsStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0)
            {
                IsStunned = false;
                if (targetAnimator != null && !IsFrozen && !IsSlowed) targetAnimator.speed = 1f;
            }
        }
    }

    public void ApplySlow(float duration) 
    { 
        IsSlowed = true; 
        slowTimer = Mathf.Max(slowTimer, duration);
        
        if (targetSprite != null && !IsFrozen) targetSprite.color = new Color(0.5f, 0.8f, 1f); 
        if (targetAnimator != null && !IsFrozen) targetAnimator.speed = 0.5f;
    }

    public void ApplyFreeze(float duration) 
    { 
        IsFrozen = true; 
        freezeTimer = Mathf.Max(freezeTimer, duration); 

        if (targetAnimator != null) targetAnimator.speed = 0f;
        if (targetSprite != null) targetSprite.color = new Color(0.2f, 0.5f, 1f); // Colder, deeper blue

        if (currentIceBlock == null && iceBlockPrefab != null)
        {
            currentIceBlock = Instantiate(iceBlockPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    public void ForceRemoveFreeze()
    {
        IsFrozen = false;
        freezeTimer = 0f;
        if (targetSprite != null) 
        {
            targetSprite.color = IsSlowed ? new Color(0.5f, 0.8f, 1f) : originalColor;
        }
        if (currentIceBlock != null) Destroy(currentIceBlock);
    }

    public void ForceRemoveBurn()
    {
        IsBurning = false;
        burnTimer = 0f;
        burnTickTimer = 0f;
        burnAttacker = null;
        if (currentBurnVFX != null) Destroy(currentBurnVFX.gameObject);
    }

    public void ApplyBurn(float duration, ParticleSystem vfxPrefab = null, GameObject attacker = null)
    { 
        IsBurning = true; 
        burnTimer = Mathf.Max(burnTimer, duration); 
        burnAttacker = attacker;

        if (currentBurnVFX == null && vfxPrefab != null)
        {
            currentBurnVFX = Instantiate(vfxPrefab, transform.position, Quaternion.identity, transform);
        }
    }

    public void ApplyPoison(float duration, int damagePerTick, GameObject attacker)
    {
        IsPoisoned = true;
        poisonTimer = Mathf.Max(poisonTimer, duration);
        poisonTickTimer = Mathf.Min(poisonTickTimer <= 0f ? 0.75f : poisonTickTimer, 0.75f);
        poisonDamage = Mathf.Max(1, damagePerTick);
        poisonAttacker = attacker;
    }
    
    public void ApplySilence(float duration) 
    { 
        IsSilenced = true; 
        silenceTimer = Mathf.Max(silenceTimer, duration); 
    }

    public void ApplyStun(float duration)
    {
        IsStunned = true;
        stunTimer = Mathf.Max(stunTimer, duration);
        if (targetAnimator != null) targetAnimator.speed = 0f;
    }

    public void ApplyInterrupt()
    {
        if (targetAnimator != null) targetAnimator.SetTrigger("Hurt");
    }
}
