using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlaystyleManager))]
public class PlayerAttack : MonoBehaviour
{
    public event EventHandler<AttackEventArgs> OnAttackStarted;
    public event EventHandler<AttackEventArgs> OnAttackEnded;
    public event EventHandler<AttackEventArgs> OnAttackCancelled;

    public class AttackEventArgs : EventArgs
    {
        public AttackType attackType;
        public PlaystyleType playstyleType;
        public int comboStep;
        public string animationName;
    }

    public enum AttackType
    {
        Basic,
        Dash,
        Air
    }

    [Header("References")]
    [SerializeField] private InputConfig inputConfig;
    [SerializeField] private AttackHitbox attackHitbox;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;

    [Header("Dash & Air Attacks")]
    [SerializeField] private float dashAttackDuration = 0.25f;
    [SerializeField] private float airAttackDuration = 0.35f;
    [SerializeField] private float dashProcCoef = 0.7f;
    [SerializeField] private float airProcCoef = 0.8f;
    [SerializeField] private Vector2 dashAttackHitboxSize = new Vector2(2f, 1f);
    [SerializeField] private Vector2 dashAttackHitboxOffset = new Vector2(1f, 0f);
    [SerializeField] private Vector2 airAttackHitboxSize = new Vector2(2f, 2f);
    [SerializeField] private Vector2 airAttackHitboxOffset = new Vector2(0f, 0f);

    [Header("Plunge Attack")]
    [SerializeField] private float plungeRadius = 2.5f;
    [SerializeField] private float plungeDamageMultiplier = 2.5f;
    [SerializeField] private GameObject plungeImpactVFX;
    [SerializeField] private float safePlungeDistance = 15f;
    [SerializeField] private float maxPlungeDistance = 30f;
    [Range(0f, 1f)]
    [SerializeField] private float maxPlungeSelfDamagePercent = 0.5f;
    [SerializeField] private Vector2 plungeFallHitboxSize = new Vector2(2f, 2f);
    [SerializeField] private Vector2 plungeFallHitboxOffset = new Vector2(0f, -1f);

    [Header("Ranged Attack")]
    [SerializeField] private Transform firePoint;

    [Header("Attack Movement")]
    [SerializeField] private float meleeLungeForce = 7f;

    [Header("Combo Reset")]
    [SerializeField] private float comboResetTime = 2f;

    public float temporaryDamageMultiplier = 1f;

    private PlaystyleManager playstyleManager;
    private HealthSystem healthSystem;
    private EntityAudioManager audioManager;
    private bool isAttacking;
    private int currentComboStep;
    private float lastAttackTime;
    private bool comboQueued;
    private PlaystyleType queuedPlaystyle;
    private int pendingDirection;

    private AttackType currentAttackType;
    private PlaystyleType currentPlaystyle;

    private int pendingDamage;
    private float pendingProcCoef;
    private bool pendingHitboxConfigured;
    private Vector2 pendingHitboxSize;
    private Vector2 pendingHitboxOffset;
    private PlaystyleType pendingStyleType;
    private GameObject pendingProjectilePrefab;
    
    private System.Collections.Generic.HashSet<IDamageable> hitDuringPlunge = new System.Collections.Generic.HashSet<IDamageable>();

    public bool IsDirectionLocked { get; private set; }

    private void Awake()
    {
        playstyleManager = GetComponent<PlaystyleManager>();
        healthSystem = GetComponent<HealthSystem>();
        audioManager = GetComponentInChildren<EntityAudioManager>();
    }

    private void Update()
    {
        HandleAttackInput();

        if (playerMovement != null)
        {
            if (playerMovement.isPlunging)
            {
                HandlePlungeFallingDamage();
            }
            else if (hitDuringPlunge.Count > 0)
            {
                hitDuringPlunge.Clear();
            }
        }
    }

    private void HandleAttackInput()
    {
        if (healthSystem != null && healthSystem.IsDead) return;
        if (playerMovement != null && playerMovement.isStunned) return;

        if (inputConfig == null) return;

        if (inputConfig.GetAttackMagicDown())
        {
            TryStartAttack(PlaystyleType.Magic);
        }
        else if (inputConfig.GetAttackLongDown())
        {
            TryStartAttack(PlaystyleType.LongRange);
        }
        else if (inputConfig.GetAttackMidDown())
        {
            TryStartAttack(PlaystyleType.MidRange);
        }
        else if (inputConfig.GetAttackMeleeDown())
        {
            if (playerMovement != null && !playerMovement.isGrounded && inputConfig.GetVerticalInput() < -0.1f)
            {
                // It's a plunge attack, let PlayerMovement handle it.
                return;
            }
            TryStartAttack(PlaystyleType.Melee);
        }
    }

    private void TryStartAttack(PlaystyleType type)
    {
        if (!playstyleManager.IsPlaystyleUnlocked(type)) return;
        if (type == PlaystyleType.Magic && !playstyleManager.CanUseMagic()) return;

        if (!isAttacking)
        {
            // Reset combo if too much time has passed or playstyle changed
            if (Time.time - lastAttackTime > comboResetTime || currentPlaystyle != type)
            {
                currentComboStep = 0;
            }
            DetermineAndExecuteAttack(type);
        }
        else
        {
            // Buffer the next attack input
            comboQueued = true;
            queuedPlaystyle = type;

            // Capture direction intent for the next combo step
            float horizontalInput = inputConfig.GetHorizontalInput();
            if (horizontalInput != 0f)
            {
                pendingDirection = horizontalInput > 0f ? 1 : -1;
            }
        }
    }

    private void DetermineAndExecuteAttack(PlaystyleType type)
    {
        if (playerMovement != null && playerMovement.isPlunging) return;
        
        // Let PlayerMovement handle Plunge initiation if holding down in air
        if (playerMovement != null && !IsGrounded() && inputConfig != null && inputConfig.GetVerticalInput() < -0.1f) return;

        AttackType attType = AttackType.Basic;
        if (playerMovement != null && playerMovement.isDashing)
        {
            attType = AttackType.Dash;
        }
        else if (playerMovement != null && !IsGrounded())
        {
            attType = AttackType.Air;
        }

        ExecuteAttack(attType, type);
    }

    private void ExecuteAttack(AttackType attType, PlaystyleType styleType)
    {
        if (isAttacking) return;
        currentAttackType = attType;
        currentPlaystyle = styleType;

        // Auto-select the echo slot corresponding to the playstyle
        int slotIndex = styleType switch
        {
            PlaystyleType.Melee => 0,
            PlaystyleType.MidRange => 1,
            PlaystyleType.LongRange => 2,
            PlaystyleType.Magic => 3,
            _ => 0
        };
        
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.SetActiveEchoIndex(slotIndex);
        }

        StartCoroutine(AttackRoutine(attType, styleType));
    }

    private IEnumerator AttackRoutine(AttackType attType, PlaystyleType styleType)
    {
        isAttacking = true;
        comboQueued = false;
        pendingDirection = 0;
        IsDirectionLocked = true;

        PlaystyleData pData = playstyleManager.GetPlaystyleData(styleType);
        if (pData == null)
        {
            CleanupAttack();
            yield break;
        }

        // Strict combo cycle: always wrap around
        if (currentComboStep >= pData.comboSteps) currentComboStep = 0;

        float dmgMult = attType == AttackType.Basic && pData.comboDamageMultipliers.Length > currentComboStep
            ? pData.comboDamageMultipliers[currentComboStep]
            : (attType == AttackType.Dash ? 1.3f : 1.1f);

        float procCoef = attType == AttackType.Basic ? pData.procCoefficient : (attType == AttackType.Dash ? dashProcCoef : airProcCoef);

        string animName = pData.attackAnimationNames != null && pData.attackAnimationNames.Length > currentComboStep
            ? pData.attackAnimationNames[currentComboStep]
            : "Attack";

        if (attType != AttackType.Basic)
        {
            animName = attType == AttackType.Dash ? "AttackDash" : "Air Attack";
            currentComboStep = 0;
        }

        var args = new AttackEventArgs
        {
            attackType = attType,
            playstyleType = styleType,
            comboStep = currentComboStep,
            animationName = animName
        };

        OnAttackStarted?.Invoke(this, args);

        // Melee lunge
        if (attType == AttackType.Basic && styleType == PlaystyleType.Melee && playerMovement != null && playerMovement.isGrounded)
        {
            float facingDir = playerMovement.isFacingRight ? 1f : -1f;
            playerMovement.rb.linearVelocity = new Vector2(0f, playerMovement.rb.linearVelocity.y);
            playerMovement.rb.AddForce(new Vector2(facingDir * meleeLungeForce, 0f), ForceMode2D.Impulse);
        }

        int baseDamage = healthSystem != null && healthSystem.CombatStats != null ? healthSystem.CombatStats.baseAttack : 10;
        int damageToPass = Mathf.RoundToInt(baseDamage * dmgMult * temporaryDamageMultiplier);

        if (styleType == PlaystyleType.Melee || styleType == PlaystyleType.MidRange || styleType == PlaystyleType.LongRange)
        {
            if (attackHitbox != null)
            {
                if (attType == AttackType.Basic && pData.hitboxSizes.Length > currentComboStep)
                {
                    pendingHitboxSize = pData.hitboxSizes[currentComboStep];
                    pendingHitboxOffset = pData.hitboxOffsets[currentComboStep];
                    pendingHitboxConfigured = true;
                }
                else if (attType == AttackType.Dash)
                {
                    pendingHitboxSize = dashAttackHitboxSize;
                    pendingHitboxOffset = dashAttackHitboxOffset;
                    pendingHitboxConfigured = true;
                }
                else if (attType == AttackType.Air)
                {
                    pendingHitboxSize = airAttackHitboxSize;
                    pendingHitboxOffset = airAttackHitboxOffset;
                    pendingHitboxConfigured = true;
                }
                else
                {
                    pendingHitboxConfigured = false;
                }
            }
            else
            {
                pendingHitboxConfigured = false;
            }

            pendingDamage = damageToPass;
            pendingProcCoef = procCoef;
            pendingStyleType = styleType;
            pendingProjectilePrefab = pData.projectilePrefab;

            // Dash and Air attacks still trigger instantly (unless you add animation events for them too)
            if (attType != AttackType.Basic)
            {
                TriggerHitbox();
            }
        }
        else if (styleType == PlaystyleType.Magic && pData.magicAoEPrefab != null)
        {
            Instantiate(pData.magicAoEPrefab, transform.position, Quaternion.identity);
            SpawnComboVFX();
        }

        // Wait for the animation to finish instead of a fixed duration
        if (attType == AttackType.Basic)
        {
            yield return WaitForAnimationEnd(animName);
        }
        else
        {
            yield return new WaitForSeconds(attType == AttackType.Dash ? dashAttackDuration : airAttackDuration);
        }

        if (attType == AttackType.Basic)
        {
            lastAttackTime = Time.time;
            currentComboStep++;
            if (currentComboStep >= pData.comboSteps) currentComboStep = 0;
        }

        CleanupAttack();
        OnAttackEnded?.Invoke(this, args);

        // Process buffered combo input
        if (comboQueued)
        {
            comboQueued = false;

            // Apply direction change between combo steps
            if (pendingDirection != 0 && playerMovement != null)
            {
                bool wantFaceRight = pendingDirection > 0;
                if (wantFaceRight != playerMovement.isFacingRight)
                {
                    playerMovement.CheckDirectionToFace(wantFaceRight);
                }
            }
            pendingDirection = 0;

            DetermineAndExecuteAttack(queuedPlaystyle);
        }
    }

    private IEnumerator WaitForAnimationEnd(string stateName)
    {
        // Wait one frame for the animator to start the state
        yield return null;

        if (animator == null) yield break;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Safety: make sure we're in the correct state
        if (!stateInfo.IsName(stateName))
        {
            // Wait a couple more frames in case there's a transition delay
            for (int i = 0; i < 3; i++)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(stateName)) break;
            }
        }

        if (stateInfo.IsName(stateName))
        {
            // Wait until the animation has played through at least once
            float clipLength = stateInfo.length;
            float elapsed = stateInfo.normalizedTime * clipLength;
            float remaining = clipLength - elapsed;
            if (remaining > 0f) yield return new WaitForSeconds(remaining);
        }
    }

    private void CleanupAttack()
    {
        isAttacking = false;
        IsDirectionLocked = false;
    }

    public void CancelAttackForMovement()
    {
        if (!isAttacking) return;

        StopAllCoroutines();
        CleanupAttack();
        comboQueued = false;
        currentComboStep = 0;
        pendingDirection = 0;

        if (attackHitbox != null) attackHitbox.Deactivate();

        OnAttackCancelled?.Invoke(this, new AttackEventArgs
        {
            attackType = currentAttackType,
            playstyleType = currentPlaystyle,
            comboStep = currentComboStep
        });
    }

    public void CancelAttack()
    {
        if (!isAttacking) return;

        StopAllCoroutines();
        CleanupAttack();
        comboQueued = false;
        currentComboStep = 0;
        pendingDirection = 0;

        if (attackHitbox != null) attackHitbox.Deactivate();

        OnAttackEnded?.Invoke(this, new AttackEventArgs
        {
            attackType = currentAttackType,
            playstyleType = currentPlaystyle,
            comboStep = currentComboStep
        });
    }

    private bool IsGrounded()
    {
        if (playerMovement != null)
        {
            return playerMovement.LastOnGroundTime > 0;
        }
        return true;
    }

    public bool IsAttacking => isAttacking;
    public int CurrentComboStep => currentComboStep;
    public AttackType CurrentAttackType => currentAttackType;
    public PlaystyleType CurrentPlaystyle => currentPlaystyle;
    public AttackHitbox CurrentAttackHitbox => attackHitbox;

    public void EnableAttackHitbox()
    {
        if (attackHitbox != null) attackHitbox.gameObject.SetActive(true);
    }

    public void DisableAttackHitbox()
    {
        if (attackHitbox != null) attackHitbox.gameObject.SetActive(false);
    }

    // Call this from an Animation Event!
    public void TriggerHitbox()
    {
        if (pendingStyleType == PlaystyleType.Melee || pendingStyleType == PlaystyleType.MidRange)
        {
            if (attackHitbox != null)
            {
                if (pendingHitboxSize != Vector2.zero)
                    attackHitbox.Configure(pendingHitboxSize, pendingHitboxOffset);

                attackHitbox.Activate(pendingDamage, pendingProcCoef);
                SpawnComboVFX();
                PlayEchoSoundIfNeeded();
            }
        }
        else if (pendingStyleType == PlaystyleType.LongRange && pendingProjectilePrefab != null)
        {
            Vector2 facingDir = playerMovement != null && playerMovement.isFacingRight ? Vector2.right : Vector2.left;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (Vector3)facingDir * 0.5f + Vector3.up * 1.0f;
            
            GameObject proj = ObjectPoolManager.SpawnObject(pendingProjectilePrefab, spawnPos, Quaternion.identity, ObjectPoolManager.PoolType.Projectile);
            proj.transform.localScale = transform.localScale;

            Projectile pScript = proj.GetComponent<Projectile>();
            if (pScript != null)
            {
                DamageInfo dInfo = new DamageInfo
                {
                    baseDamage = pendingDamage,
                    multiplicativeStack = 1f,
                    procCoefficient = pendingProcCoef,
                    attacker = gameObject,
                    damageSource = DamageSourceType.PlayerProjectile,
                    isCritical = false
                };
                
                if (PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.ActiveEcho != null)
                {
                    dInfo.activeEcho = PlayerInventoryCore.Instance.ActiveEcho;
                    if (PlayerStats.Instance != null) dInfo.playerLevel = PlayerStats.Instance.CurrentLevel;
                }
                
                if (healthSystem != null && healthSystem.CombatStats != null && UnityEngine.Random.value < healthSystem.CombatStats.critChance)
                {
                    dInfo.isCritical = true;
                    dInfo.multiplicativeStack *= healthSystem.CombatStats.critMultiplier;
                }

                // Fire the projectile at 20 units/sec
                pScript.SetupPlayerProjectile(dInfo, facingDir * 20f);
            }
            
            SpawnComboVFX();
            PlayEchoSoundIfNeeded();
        }
    }

    private void PlayEchoSoundIfNeeded()
    {
        if (audioManager != null && PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.ActiveEcho != null)
        {
            audioManager.PlaySound("Echo Attack");
        }
    }

    private void SpawnComboVFX()
    {
        PlaystyleData pData = playstyleManager.GetPlaystyleData(currentPlaystyle);
        if (pData == null) return;

        // Only spawn VFX if the playstyle requires an active Echo and we have one, 
        // OR if it doesn't require an active Echo (normal attacks).
        if (pData.requiresActiveEcho && (PlayerInventoryCore.Instance == null || PlayerInventoryCore.Instance.ActiveEcho == null))
            return;

        if (pData.comboVFXPrefabs != null && pData.comboVFXPrefabs.Length > 0)
        {
            // Fallback to Echo 0 if the current combo step doesn't have a specific prefab assigned
            GameObject vfxPrefab = null;
            Vector2 vfxOffset = Vector2.zero;

            if (currentComboStep < pData.comboVFXPrefabs.Length && pData.comboVFXPrefabs[currentComboStep] != null)
            {
                vfxPrefab = pData.comboVFXPrefabs[currentComboStep];
            }
            else
            {
                vfxPrefab = pData.comboVFXPrefabs[0];
            }

            // Get the specific offset for this combo step
            if (pData.vfxOffsets != null && currentComboStep < pData.vfxOffsets.Length)
            {
                vfxOffset = pData.vfxOffsets[currentComboStep];
            }
            else if (pData.vfxOffsets != null && pData.vfxOffsets.Length > 0)
            {
                vfxOffset = pData.vfxOffsets[0]; // Fallback to first offset
            }

            if (vfxPrefab != null)
            {
                // Calculate spawn position based on player facing direction and the specific VFX offset
                float dirMultiplier = playerMovement != null && playerMovement.isFacingRight ? 1f : -1f;
                Vector3 spawnPos = transform.position + new Vector3(vfxOffset.x * dirMultiplier, vfxOffset.y, 0f);
                
                GameObject vfx = ObjectPoolManager.SpawnObject(vfxPrefab, spawnPos, vfxPrefab.transform.rotation, ObjectPoolManager.PoolType.ParticleSystem);
                
                // Flip the VFX to match player facing direction
                Vector3 vfxScale = vfx.transform.localScale;
                vfxScale.x = playerMovement != null && playerMovement.isFacingRight ? Mathf.Abs(vfxScale.x) : -Mathf.Abs(vfxScale.x);
                vfx.transform.localScale = vfxScale;

                // Tell the VFX which combo step it is playing
                ComboVFXController vfxController = vfx.GetComponent<ComboVFXController>();
                if (vfxController != null)
                {
                    vfxController.PlayComboStep(currentComboStep);
                }
            }
        }
    }

    public void StartPlungeFallHitbox()
    {
        if (attackHitbox != null && healthSystem != null && healthSystem.CombatStats != null)
        {
            attackHitbox.Configure(plungeFallHitboxSize, plungeFallHitboxOffset); 
            attackHitbox.isPlungeFalling = true;
        }
    }

    public void StopPlungeFallHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.isPlungeFalling = false;
        }
    }


    public void ExecutePlungeAOE(float dropDistance)
    {
        int baseDamage = healthSystem != null && healthSystem.CombatStats != null ? healthSystem.CombatStats.baseAttack : 10;

        float echoDamageMult = 1f;
        if (PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.ActiveEcho != null)
        {
            echoDamageMult = PlayerInventoryCore.Instance.ActiveEcho.baseDamageMultiplier;
        }

        int finalDamage = Mathf.RoundToInt(baseDamage * plungeDamageMultiplier * echoDamageMult);

        LayerMask targetMask = attackHitbox != null ? attackHitbox.TargetLayers : LayerMask.GetMask("Enemy");
        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 originPos = attackHitbox != null ? (Vector2)attackHitbox.transform.position : (Vector2)transform.position;
        Vector2 center = originPos + new Vector2(plungeFallHitboxOffset.x * direction.x, plungeFallHitboxOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, plungeFallHitboxSize, 0f, targetMask);

        foreach (Collider2D col in hits)
        {
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(new DamageInfo
                {
                    baseDamage       = finalDamage,
                    multiplicativeStack = 1f,
                    procCoefficient  = 1f,
                    attacker         = gameObject,
                    knockbackForce   = 0f,
                    knockbackDirection = Vector2.zero,
                    damageSource     = DamageSourceType.PlungeAttack,
                });
                
                EchoStatusReceiver status = col.GetComponentInParent<EchoStatusReceiver>();
                if (status != null)
                {
                    status.ApplyStun(1f);
                }
            }
        }

        if (plungeImpactVFX != null)
            Instantiate(plungeImpactVFX, transform.position, Quaternion.identity);

        if (dropDistance > safePlungeDistance && healthSystem != null)
        {
            float exceedDistance = dropDistance - safePlungeDistance;
            float maxExceed = maxPlungeDistance - safePlungeDistance;
            float damagePercent = Mathf.Clamp01(exceedDistance / maxExceed) * maxPlungeSelfDamagePercent;
            
            int selfDamage = Mathf.CeilToInt(healthSystem.MaxHP * damagePercent);
            if (selfDamage > 0)
            {
                Debug.Log($"[PlungeSelfDamage] Dealt {selfDamage} damage. DropDistance: {dropDistance}, SafeDist: {safePlungeDistance}, MaxHP: {healthSystem.MaxHP}");
                healthSystem.TakeDamage(new DamageInfo
                {
                    baseDamage = selfDamage,
                    attacker = gameObject,
                    damageSource = DamageSourceType.PlungeSelfDamage,
                    knockbackForce = 0f,
                    knockbackDirection = Vector2.zero
                });
            }
        }
    }

    private void HandlePlungeFallingDamage()
    {
        LayerMask targetMask = attackHitbox != null ? attackHitbox.TargetLayers : LayerMask.GetMask("Enemy");
        Vector2 direction = transform.localScale.x >= 0 ? Vector2.right : Vector2.left;
        Vector2 originPos = attackHitbox != null ? (Vector2)attackHitbox.transform.position : (Vector2)transform.position;
        Vector2 center = originPos + new Vector2(plungeFallHitboxOffset.x * direction.x, plungeFallHitboxOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, plungeFallHitboxSize, 0f, targetMask);
        foreach (var col in hits)
        {
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                if (!hitDuringPlunge.Contains(damageable) && !damageable.IsDead)
                {
                    hitDuringPlunge.Add(damageable);

                    int baseDamage = healthSystem != null && healthSystem.CombatStats != null ? healthSystem.CombatStats.baseAttack : 10;
                    float echoDamageMult = 1f;
                    if (PlayerInventoryCore.Instance != null && PlayerInventoryCore.Instance.ActiveEcho != null)
                    {
                        echoDamageMult = PlayerInventoryCore.Instance.ActiveEcho.baseDamageMultiplier;
                    }

                    int finalDamage = Mathf.RoundToInt(baseDamage * plungeDamageMultiplier * echoDamageMult);

                    damageable.TakeDamage(new DamageInfo
                    {
                        baseDamage = finalDamage,
                        multiplicativeStack = 1f,
                        procCoefficient = 0.5f,
                        attacker = gameObject,
                        knockbackForce = 0f,
                        knockbackDirection = Vector2.zero,
                        damageSource = DamageSourceType.PlungeFall
                    });
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f); // Cyan
        Gizmos.DrawWireSphere(transform.position, plungeRadius);

#if UNITY_EDITOR
        if (playstyleManager != null)
        {
            PlaystyleData pData = playstyleManager.GetPlaystyleData(currentPlaystyle);
            if (pData != null && pData.vfxOffsets != null)
            {
                // Draw gizmos assuming the player is facing right
                for (int i = 0; i < pData.vfxOffsets.Length; i++)
                {
                    Vector2 offset = pData.vfxOffsets[i];
                    Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);
                    
                    // Draw a colored sphere for each combo step
                    if (i == 0) Gizmos.color = Color.red;       // Combo 1
                    else if (i == 1) Gizmos.color = Color.green; // Combo 2
                    else if (i == 2) Gizmos.color = Color.blue;  // Combo 3
                    else Gizmos.color = Color.yellow;

                    Gizmos.DrawWireSphere(spawnPos, 0.2f);
                    
                    // Optional: Draw a line from player center to the spawn point
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                    Gizmos.DrawLine(transform.position, spawnPos);
                }
            }
        }
#endif
    }
}
