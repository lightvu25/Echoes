using UnityEngine;
using System.Collections.Generic;

public class EchoEffectManager : MonoBehaviour
{
    private AttackHitbox playerAttackHitbox;
    private HealthSystem playerHealth;

    [Header("Fusion Prefabs")]
    [SerializeField] private GameObject eventHorizonPrefab;
    [SerializeField] private GameObject fireTrailPrefab;
    [SerializeField] private GameObject glitchedZonePrefab;
    private float blackHoleCooldownTimer = 0f;

    private void Awake()
    {
        playerAttackHitbox = GetComponentInChildren<AttackHitbox>();
        playerHealth = GetComponent<HealthSystem>();
    }

    private void Update()
    {
        if (blackHoleCooldownTimer > 0) blackHoleCooldownTimer -= Time.deltaTime;
    }

    private void OnEnable()
    {
        if (playerAttackHitbox != null)
        {
            playerAttackHitbox.OnBeforeDamageApplied += HandleBeforeDamageApplied;
            playerAttackHitbox.OnHitTarget += HandleOnHitTarget;
        }
        if (playerHealth != null)
        {
            playerHealth.OnBeforeTakeDamage += HandleDefensiveModifiers;
        }
    }

    private void OnDisable()
    {
        // Hủy đăng ký để tránh rò rỉ bộ nhớ (Memory Leak)
        if (playerAttackHitbox != null)
        {
            playerAttackHitbox.OnBeforeDamageApplied -= HandleBeforeDamageApplied;
            playerAttackHitbox.OnHitTarget -= HandleOnHitTarget;
        }
        if (playerHealth != null)
        {
            playerHealth.OnBeforeTakeDamage -= HandleDefensiveModifiers;
        }
    }

    // ==========================================
    // TRẠM 1: TRƯỚC KHI TÍNH SÁT THƯƠNG
    // ==========================================
    private void HandleBeforeDamageApplied(IDamageable target, ref DamageInfo damageInfo)
    {
        if (damageInfo.activeEcho == null) return;
        string modifierID = damageInfo.activeEcho.uniqueModifierID;

        // BLAZE - Ignition: Sát thương tăng theo % máu đã mất của quái
        if (modifierID == "IGNITION")
        {
            HealthSystem targetHealth = target.Transform.GetComponent<HealthSystem>();
            if (targetHealth != null)
            {
                // Lấy % máu đã mất
                // Giả sử quái mất 50% máu -> Tăng thêm 10% * 0.5 = 5% sát thương tổng
                float missingHpPercent = 1f - ((float)targetHealth.CurrentHP / targetHealth.MaxHP);
                damageInfo.multiplicativeStack *= (1f + (0.10f * missingHpPercent)); 
            }
        }

        // ANOMALY - Distortion: 25% tỷ lệ chuyển thành Sát thương chuẩn
        else if (modifierID == "DISTORTION")
        {
            if (UnityEngine.Random.value <= 0.25f)
            {
                damageInfo.isTrueDamage = true;
                // Có thể gọi thêm VFX nhiễu sóng (Glitch) tại đây
            }
        }
        else if (modifierID == "FUS_ENTROPY")
        {
            damageInfo.isTrueDamage = true;
            // Entropy randomizes the attack hitbox size dynamically. 
            // Assuming the attack hitbox Transform is available via the event sender.
            float randomScale = UnityEngine.Random.Range(0.5f, 2.5f);
            // You will need to instruct the player to apply this scale to the hitbox parent during the attack animation.
        }
        
        // ICE - Frostbite & Cryo-Stasis logic
        EchoStatusReceiver status = target.Transform.GetComponent<EchoStatusReceiver>();
        if (status == null) status = target.Transform.gameObject.AddComponent<EchoStatusReceiver>();

        if (modifierID == "FROSTBITE" && status.IsSlowed)
        {
            if (!damageInfo.isCritical && UnityEngine.Random.value <= 0.30f) 
            { 
                damageInfo.isCritical = true; 
                damageInfo.multiplicativeStack *= 1.5f; 
            }
        }
        else if (modifierID == "FUS_CRYO_STASIS")
        {
            damageInfo.knockbackForce = 0f;
            if (status.IsFrozen)
            {
                damageInfo.baseDamage = 99999;
                damageInfo.isTrueDamage = true;
            }
        }
    }

    // ==========================================
    // TRẠM 2: SAU KHI ĐÁNH TRÚNG ĐÍCH
    // ==========================================
    private void HandleOnHitTarget(object sender, AttackHitbox.HitEventArgs e)
    {
        if (e.damageInfo.activeEcho == null) return;
        string modifierID = e.damageInfo.activeEcho.uniqueModifierID;

        // Avoid infinite loop from secondary damage sources
        if (e.damageInfo.damageSource == "ArcChain" || e.damageInfo.damageSource == "BlackHole") return;

        EchoStatusReceiver status = e.target.Transform.GetComponent<EchoStatusReceiver>();
        if (status == null) status = e.target.Transform.gameObject.AddComponent<EchoStatusReceiver>();

        float baseChance = e.damageInfo.activeEcho.statusProcCoefficient;
        float currentProc = e.damageInfo.procCoefficient;
        bool procSuccessful = DamageCalculator.ShouldProc(baseChance, currentProc);

        switch (modifierID)
        {
            case "IGNITION":
                if (procSuccessful) status.ApplyBurn(3f);
                break;

            case "CHAIN_ARC":
                ExecuteArcChain(e.target.Transform.position, e.damageInfo, e.target);
                if (procSuccessful) status.ApplyInterrupt();
                break;

            case "KINETIC_FORCE":
                if (procSuccessful) status.ApplyStun(1.5f);
                break;

            case "FROSTBITE":
                if (procSuccessful) status.ApplySlow(3f);
                break;

            case "FUS_CRYO_STASIS":
                if (procSuccessful) status.ApplyFreeze(2f);
                break;

            case "OBLIVION":
                int selfDamage = Mathf.Max(1, Mathf.RoundToInt(playerHealth.MaxHP * 0.02f));
                DamageInfo curseDamage = DamageInfo.Create(selfDamage, gameObject);
                curseDamage.isTrueDamage = true;
                playerHealth.TakeDamage(curseDamage);
                break;

            case "FUS_EVENT_HORIZON":
                if (blackHoleCooldownTimer <= 0f && eventHorizonPrefab != null)
                {
                    GameObject blackHole = Instantiate(eventHorizonPrefab, e.target.Transform.position, Quaternion.identity);
                    BlackHoleEffect bhEffect = blackHole.GetComponent<BlackHoleEffect>();
                    if (bhEffect != null) bhEffect.Initialize(gameObject);
                    blackHoleCooldownTimer = 3f;
                }
                break;

            case "FUS_NEON_GRID":
                if (glitchedZonePrefab != null)
                    Instantiate(glitchedZonePrefab, e.target.Transform.position, Quaternion.identity);
                break;
        }
    }

    // --- Logic Thuật Toán Quét Mục Tiêu Của ARC ---
    private void ExecuteArcChain(Vector2 origin, DamageInfo originalInfo, IDamageable originalTarget)
    {
        float chainRadius = 5f;
        int maxBounces = 2;
        LayerMask enemyLayer = LayerMask.GetMask("Enemy");

        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, chainRadius, enemyLayer);
        int hitCount = 0;

        foreach (Collider2D col in colliders)
        {
            if (hitCount >= maxBounces) break;

            IDamageable nextTarget = col.GetComponent<IDamageable>();
            if (nextTarget != null && nextTarget.Transform != originalTarget.Transform) 
            {
                DamageInfo chainInfo = DamageInfo.Create(Mathf.RoundToInt(originalInfo.baseDamage * 0.5f), gameObject);
                chainInfo.damageSource = "ArcChain"; // Gắn tag để không bị nảy vô hạn
                chainInfo.activeEcho = originalInfo.activeEcho;

                nextTarget.TakeDamage(chainInfo);
                
                hitCount++;
            }
        }
    }

    private void HandleDefensiveModifiers(ref int damageAmount, ref DamageInfo info)
    {
        string activeModifier = PlayerInventoryCore.Instance?.ActiveEcho?.uniqueModifierID ?? "";

        if (activeModifier == "FUS_RAGNAROK" && damageAmount > 0)
        {
            damageAmount = 99999;
            info.isTrueDamage = true;
            Debug.Log("Ragnarok triggered: Instant Death!");
        }
    }

    public void HandlePlayerDash(Vector3 startPos, Vector3 endPos)
    {
        string activeModifier = PlayerInventoryCore.Instance?.ActiveEcho?.uniqueModifierID ?? "";
        
        if (activeModifier == "FUS_AFTERBURNER" && fireTrailPrefab != null)
        {
             // Spawn fire trails along the path
             int trails = 3;
             for(int i = 0; i <= trails; i++) 
             {
                 Vector3 pos = Vector3.Lerp(startPos, endPos, (float)i/trails);
                 Instantiate(fireTrailPrefab, pos, Quaternion.identity);
             }
        }
    }
}