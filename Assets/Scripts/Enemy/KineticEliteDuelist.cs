using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyBrain), typeof(EnemyCombat), typeof(HealthSystem))]
[RequireComponent(typeof(MeleeAttack))]
public class KineticEliteDuelist : MonoBehaviour
{
    private const float IndicatorHeightPadding = 0.65f;
    private static readonly Color KineticAccent = new Color(0.55f, 0.9f, 1f, 1f);

    [Header("Echo Identity")]
    [SerializeField] private EchoData echoData;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField, Min(0.1f)] private float echoIconWorldSize = 0.55f;

    [Header("Duelist Rhythm")]
    [SerializeField, Min(1)] private int empoweredAttackCadence = 3;
    [SerializeField, Min(1f)] private float normalKnockbackMultiplier = 1.25f;
    [SerializeField, Min(1f)] private float empoweredDamageMultiplier = 1.35f;
    [SerializeField, Min(1f)] private float empoweredKnockbackMultiplier = 2.25f;

    [Header("Reward")]
    [SerializeField] private bool guaranteedEchoDrop = true;
    [SerializeField] private Vector2 rewardPopForce = new Vector2(2.5f, 7f);

    private EnemyBrain brain;
    private MeleeAttack meleeAttack;
    private HealthSystem healthSystem;
    private SpriteRenderer echoIconRenderer;
    private SpriteRenderer echoGlowRenderer;
    private GameObject echoIndicator;
    private GameObject echoGlow;
    private int preparedAttackCount;
    private bool attackPrepared;
    private bool empoweredAttackPrepared;
    private bool rewardDropped;

    public EchoData EchoData => echoData;
    public bool IsEmpoweredAttackPrepared => empoweredAttackPrepared;

    private void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        meleeAttack = GetComponent<MeleeAttack>();
        healthSystem = GetComponent<HealthSystem>();

        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        CreateEchoIndicator();
        RefreshEchoVisual();
    }

    private void OnEnable()
    {
        if (brain != null) brain.OnStateChanged += HandleBrainStateChanged;
        if (meleeAttack != null)
        {
            meleeAttack.OnBeforeDamageApplied += HandleBeforeDamageApplied;
            meleeAttack.OnAttackFinished += HandleAttackFinished;
        }
        if (healthSystem != null) healthSystem.OnDeath += HandleDeath;

        preparedAttackCount = 0;
        rewardDropped = false;
        ResetPreparedAttack();
        RefreshEchoVisual();
    }

    private void OnDisable()
    {
        if (brain != null) brain.OnStateChanged -= HandleBrainStateChanged;
        if (meleeAttack != null)
        {
            meleeAttack.OnBeforeDamageApplied -= HandleBeforeDamageApplied;
            meleeAttack.OnAttackFinished -= HandleAttackFinished;
        }
        if (healthSystem != null) healthSystem.OnDeath -= HandleDeath;

        ResetPreparedAttack();
    }

    private void LateUpdate()
    {
        if (echoIndicator == null || !echoIndicator.activeSelf || bodyRenderer == null) return;

        Bounds bounds = bodyRenderer.bounds;
        Vector3 indicatorPosition = new Vector3(
            bounds.center.x,
            bounds.max.y + IndicatorHeightPadding,
            bodyRenderer.transform.position.z);
        echoIndicator.transform.position = indicatorPosition;

        float pulseSpeed = empoweredAttackPrepared ? 9f : 2.5f;
        float pulseAmount = empoweredAttackPrepared ? 0.18f : 0.05f;
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        SetWorldScale(echoIndicator.transform, echoIconWorldSize * pulse);

        if (echoGlow != null)
        {
            echoGlow.SetActive(empoweredAttackPrepared);
            if (empoweredAttackPrepared)
            {
                echoGlow.transform.position = indicatorPosition;
                SetWorldScale(echoGlow.transform, echoIconWorldSize * pulse * 1.45f);
                float alpha = 0.18f + Mathf.PingPong(Time.time * 0.8f, 0.22f);
                echoGlowRenderer.color = new Color(KineticAccent.r, KineticAccent.g, KineticAccent.b, alpha);
            }
        }
    }

    private void HandleBrainStateChanged(object sender, EnemyBrain.OnStateArgs args)
    {
        if (args.state == EnemyBrain.State.Telegraph)
        {
            PrepareNextAttack();
            return;
        }

        bool isAttackSequenceState = args.state == EnemyBrain.State.Attack || args.state == EnemyBrain.State.Backstep;
        if (!isAttackSequenceState && meleeAttack != null && !meleeAttack.IsAttacking)
            ResetPreparedAttack();
    }

    private void PrepareNextAttack()
    {
        if (attackPrepared) return;

        preparedAttackCount++;
        attackPrepared = true;
        empoweredAttackPrepared = KineticDuelistRules.IsEmpoweredAttack(preparedAttackCount, empoweredAttackCadence);
    }

    private void HandleBeforeDamageApplied(IDamageable target, ref DamageInfo damageInfo)
    {
        KineticDuelistRules.ApplyAttackModifiers(
            ref damageInfo,
            echoData,
            empoweredAttackPrepared,
            normalKnockbackMultiplier,
            empoweredDamageMultiplier,
            empoweredKnockbackMultiplier);
    }

    private void HandleAttackFinished(object sender, EventArgs args)
    {
        ResetPreparedAttack();
    }

    private void ResetPreparedAttack()
    {
        attackPrepared = false;
        empoweredAttackPrepared = false;
        if (echoGlow != null) echoGlow.SetActive(false);
    }

    private void HandleDeath(object sender, EventArgs args)
    {
        if (!guaranteedEchoDrop || rewardDropped || echoData == null || echoData.dropPrefab == null) return;

        rewardDropped = true;
        Vector3 origin = transform.position + Vector3.up * 0.6f;
        GameObject reward = Instantiate(echoData.dropPrefab, origin, Quaternion.identity);

        float horizontalDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        Vector2 initialForce = new Vector2(
            Mathf.Abs(rewardPopForce.x) * horizontalDirection,
            Mathf.Abs(rewardPopForce.y));

        if (reward.TryGetComponent(out ItemDrop itemDrop))
        {
            itemDrop.Initialize(initialForce, echoData);
        }
        else if (reward.TryGetComponent(out Rigidbody2D rewardBody))
        {
            rewardBody.AddForce(initialForce, ForceMode2D.Impulse);
        }
    }

    private void CreateEchoIndicator()
    {
        if (echoIndicator != null) return;

        echoGlow = new GameObject("Kinetic Echo Glow", typeof(SpriteRenderer));
        echoGlow.layer = gameObject.layer;
        echoGlow.transform.SetParent(transform, true);
        echoGlowRenderer = echoGlow.GetComponent<SpriteRenderer>();

        echoIndicator = new GameObject("Kinetic Echo Icon", typeof(SpriteRenderer));
        echoIndicator.layer = gameObject.layer;
        echoIndicator.transform.SetParent(transform, true);
        echoIconRenderer = echoIndicator.GetComponent<SpriteRenderer>();

        int sortingLayer = bodyRenderer != null ? bodyRenderer.sortingLayerID : 0;
        int sortingOrder = bodyRenderer != null ? bodyRenderer.sortingOrder + 21 : 21;
        echoGlowRenderer.sortingLayerID = sortingLayer;
        echoGlowRenderer.sortingOrder = sortingOrder - 1;
        echoIconRenderer.sortingLayerID = sortingLayer;
        echoIconRenderer.sortingOrder = sortingOrder;
    }

    private void RefreshEchoVisual()
    {
        if (echoIndicator == null) return;

        Sprite icon = echoData != null ? echoData.itemIcon : null;
        echoIconRenderer.sprite = icon;
        echoIconRenderer.color = Color.white;
        echoGlowRenderer.sprite = icon;
        echoIndicator.SetActive(icon != null);
        echoGlow.SetActive(false);
    }

    private void SetWorldScale(Transform target, float worldSize)
    {
        Vector3 parentScale = transform.lossyScale;
        target.localScale = new Vector3(
            worldSize / Mathf.Max(Mathf.Abs(parentScale.x), 0.001f) * Mathf.Sign(parentScale.x),
            worldSize / Mathf.Max(Mathf.Abs(parentScale.y), 0.001f) * Mathf.Sign(parentScale.y),
            1f);
    }

    private void OnValidate()
    {
        empoweredAttackCadence = Mathf.Max(1, empoweredAttackCadence);
        normalKnockbackMultiplier = Mathf.Max(1f, normalKnockbackMultiplier);
        empoweredDamageMultiplier = Mathf.Max(1f, empoweredDamageMultiplier);
        empoweredKnockbackMultiplier = Mathf.Max(normalKnockbackMultiplier, empoweredKnockbackMultiplier);
        echoIconWorldSize = Mathf.Max(0.1f, echoIconWorldSize);
    }
}

public static class KineticDuelistRules
{
    public static bool IsEmpoweredAttack(int attackNumber, int cadence)
    {
        return attackNumber > 0 && cadence > 0 && attackNumber % cadence == 0;
    }

    public static void ApplyAttackModifiers(
        ref DamageInfo damageInfo,
        EchoData echoData,
        bool empowered,
        float normalKnockbackMultiplier,
        float empoweredDamageMultiplier,
        float empoweredKnockbackMultiplier)
    {
        if (echoData == null) return;

        damageInfo.activeEcho = echoData;
        damageInfo.knockbackForce *= empowered
            ? Mathf.Max(1f, empoweredKnockbackMultiplier)
            : Mathf.Max(1f, normalKnockbackMultiplier);

        if (!empowered) return;

        float currentMultiplier = damageInfo.multiplicativeStack <= 0f ? 1f : damageInfo.multiplicativeStack;
        damageInfo.multiplicativeStack = currentMultiplier * Mathf.Max(1f, empoweredDamageMultiplier);
        damageInfo.isPiercing = true;
        damageInfo.hitFreezeTime = Mathf.Max(damageInfo.hitFreezeTime, 0.06f);
    }
}
