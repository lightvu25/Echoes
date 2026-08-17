using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyVisual : MonoBehaviour, IFeedbackProvider
{
    private const float SharedVisionIconWorldScale = 0.65f;
    private const float SharedVisionIconHeightPadding = 0.25f;
    private const float SharedVisionPulseSpeed = 3f;
    private const float SharedVisionPulseAmount = 0.08f;

    private GroundMovement groundMovement;
    private EnemyBrain brain;
    private EnemySensor sensor;
    private EnemyCombat enemyCombat;

    [SerializeField] private GameObject noticeIconPrefab;
    [SerializeField] private Transform iconSpawnPoint;
    [SerializeField] private float noticeCooldown;
    [SerializeField] private Animator animator;

    [SerializeField] private ParticleSystem hitParticlePrefab;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float hitFreezeDuration = 0.05f;

    private bool hasShownNotice = false;
    private float lastNoticeTime = -10f;
    private Tween alertReturnTween;
    private GameObject sharedVisionIndicator;
    private SpriteRenderer sharedVisionRenderer;
    private Vector3 sharedVisionBaseScale;
    private readonly HashSet<object> weakPointSources = new HashSet<object>();
    private GameObject weakPointIndicator;
    private SpriteRenderer weakPointRenderer;

    private SpriteColorFlasher colorFlasher;

    public Vector3 PromptOffset => iconSpawnPoint != null ? iconSpawnPoint.localPosition : Vector3.up * 2f;

    private void Awake()
    {
        groundMovement = GetComponent<GroundMovement>();
        brain = GetComponent<EnemyBrain>();
        sensor = GetComponent<EnemySensor>();
        enemyCombat = GetComponent<EnemyCombat>();
        colorFlasher = GetComponentInChildren<SpriteColorFlasher>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private bool hasIsRunning;
    private bool hasIsPatroling;
    private bool hasIsAttacking;

    private void Start()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "isRunning") hasIsRunning = true;
                if (param.name == "isPatroling") hasIsPatroling = true;
                if (param.name == "isAttacking") hasIsAttacking = true;
            }
        }

        if (groundMovement != null)
        {
            groundMovement.OnPatrol += Movement_OnPatrol;
            groundMovement.OnIdle += Movement_OnIdle;
        }

        if (brain != null)
        {
            brain.OnAttack += Brain_OnAttack;
            brain.OnNotice += Brain_OnNotice;
            brain.OnStateChanged += Brain_OnStateChanged;
        }

        if (enemyCombat != null)
        {
            enemyCombat.OnEnemyDied += EnemyCombat_OnEnemyDied;
            enemyCombat.OnDamageReceived += EnemyCombat_OnDamageReceived;
        }
    }

    private void Update()
    {
        if (hasShownNotice && sensor != null && sensor.IsPlayerOutsideVision())
            hasShownNotice = false;

        if (sharedVisionIndicator != null && sharedVisionIndicator.activeSelf)
        {
            sharedVisionIndicator.transform.position = GetSharedVisionWorldPosition();
            float pulse = 1f + Mathf.Sin(Time.time * SharedVisionPulseSpeed) * SharedVisionPulseAmount;
            sharedVisionIndicator.transform.localScale = sharedVisionBaseScale * pulse;
        }

        if (weakPointRenderer != null && weakPointIndicator.activeSelf && spriteRenderer != null)
        {
            weakPointRenderer.sprite = spriteRenderer.sprite;
            weakPointRenderer.flipX = spriteRenderer.flipX;
            weakPointRenderer.flipY = spriteRenderer.flipY;
        }
    }

    private void OnDisable()
    {
        if (sharedVisionIndicator != null)
        {
            sharedVisionIndicator.SetActive(false);
        }
        weakPointSources.Clear();
        RefreshWeakPointVisual();
    }

    public void SetSharedVisionIcon(Sprite icon)
    {
        if (icon == null)
        {
            if (sharedVisionIndicator != null)
            {
                sharedVisionIndicator.SetActive(false);
            }
            return;
        }

        if (sharedVisionIndicator == null)
        {
            sharedVisionIndicator = new GameObject("Shared Vision Eye");
            sharedVisionIndicator.layer = gameObject.layer;
            sharedVisionIndicator.transform.SetParent(transform, true);

            sharedVisionRenderer = sharedVisionIndicator.AddComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                sharedVisionRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                sharedVisionRenderer.sortingOrder = spriteRenderer.sortingOrder + 20;
            }
            else
            {
                sharedVisionRenderer.sortingOrder = 20;
            }

            Vector3 parentScale = transform.lossyScale;
            sharedVisionBaseScale = new Vector3(
                SharedVisionIconWorldScale / Mathf.Max(Mathf.Abs(parentScale.x), 0.001f),
                SharedVisionIconWorldScale / Mathf.Max(Mathf.Abs(parentScale.y), 0.001f),
                1f);
        }

        sharedVisionRenderer.sprite = icon;
        sharedVisionIndicator.transform.position = GetSharedVisionWorldPosition();
        sharedVisionIndicator.transform.localScale = sharedVisionBaseScale;
        sharedVisionIndicator.SetActive(true);
    }

    public void SetWeakPointHighlighted(object source, bool highlighted)
    {
        if (source == null) return;
        if (highlighted) weakPointSources.Add(source);
        else weakPointSources.Remove(source);
        RefreshWeakPointVisual();
    }

    private void RefreshWeakPointVisual()
    {
        if (weakPointSources.Count == 0)
        {
            if (weakPointIndicator != null) weakPointIndicator.SetActive(false);
            return;
        }
        if (spriteRenderer == null) return;
        if (weakPointIndicator == null)
        {
            weakPointIndicator = new GameObject("Weak Point Highlight");
            weakPointIndicator.layer = gameObject.layer;
            weakPointIndicator.transform.SetParent(spriteRenderer.transform, false);
            weakPointIndicator.transform.localScale = Vector3.one * 1.08f;
            weakPointRenderer = weakPointIndicator.AddComponent<SpriteRenderer>();
            weakPointRenderer.material = spriteRenderer.material;
            weakPointRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            weakPointRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            weakPointRenderer.color = new Color(1f, 0.08f, 0.02f, 0.55f);
        }
        weakPointRenderer.sprite = spriteRenderer.sprite;
        weakPointIndicator.SetActive(true);
    }

    private Vector3 GetSharedVisionWorldPosition()
    {
        if (spriteRenderer == null)
        {
            return transform.position + Vector3.up * 2f;
        }

        Bounds bodyBounds = spriteRenderer.bounds;
        return new Vector3(
            bodyBounds.center.x,
            bodyBounds.max.y + SharedVisionIconHeightPadding,
            spriteRenderer.transform.position.z);
    }

    private void OnDestroy()
    {
        if (groundMovement != null)
        {
            groundMovement.OnPatrol -= Movement_OnPatrol;
            groundMovement.OnIdle -= Movement_OnIdle;
        }
        if (brain != null)
        {
            brain.OnAttack -= Brain_OnAttack;
            brain.OnNotice -= Brain_OnNotice;
            brain.OnStateChanged -= Brain_OnStateChanged;
        }
        if (enemyCombat != null)
        {
            enemyCombat.OnEnemyDied -= EnemyCombat_OnEnemyDied;
            enemyCombat.OnDamageReceived -= EnemyCombat_OnDamageReceived;
        }
    }

    private void Movement_OnPatrol(object sender, EventArgs e)
    {
        if (hasIsRunning) animator.SetBool("isRunning", false);
        if (hasIsPatroling) animator.SetBool("isPatroling", true);
    }

    private void Movement_OnIdle(object sender, EventArgs e)
    {
        if (hasIsRunning) animator.SetBool("isRunning", false);
        if (hasIsPatroling) animator.SetBool("isPatroling", false);
    }

    private void Brain_OnAttack(object sender, EventArgs e)
    {
        if (hasIsAttacking) animator.SetTrigger("isAttacking");
        if (hasIsPatroling) animator.SetBool("isPatroling", false);
        if (hasIsRunning) animator.SetBool("isRunning", false);
    }

    private void Brain_OnStateChanged(object sender, EnemyBrain.OnStateArgs e)
    {
        if (e.state == EnemyBrain.State.Attack)
        {
            animator.speed = brain.Data.attackSpeed > 0 ? brain.Data.attackSpeed : 1f;
        }
        else
        {
            animator.speed = 1f;
        }
    }

    private void Brain_OnNotice(object sender, EventArgs e)
    {
        if (!hasShownNotice && Time.time >= lastNoticeTime + noticeCooldown)
        {
            if (noticeIconPrefab != null)
            {
                GameObject spawnedIcon = ObjectPoolManager.SpawnObject(
                    noticeIconPrefab,
                    transform.position + PromptOffset,
                    Quaternion.identity,
                    ObjectPoolManager.PoolType.UI
                );

                if (spawnedIcon != null)
                {
                    spawnedIcon.transform.SetParent(transform);
                    spawnedIcon.transform.localPosition = PromptOffset;

                    Vector3 pScale = transform.lossyScale;
                    spawnedIcon.transform.localScale = new Vector3(
                        pScale.x != 0 ? 1f / Mathf.Abs(pScale.x) : 1f,
                        pScale.y != 0 ? 1f / Mathf.Abs(pScale.y) : 1f,
                        pScale.z != 0 ? 1f / Mathf.Abs(pScale.z) : 1f
                    );
                }
            }

            hasShownNotice = true;
            lastNoticeTime = Time.time;
        }
    }

    private void EnemyCombat_OnEnemyDied(object sender, EventArgs e)
    {
        StartCoroutine(DestroyRoutine());
    }

    private IEnumerator DestroyRoutine()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach(var col in colliders) col.enabled = false;

        if (brain != null) brain.enabled = false;
        if (groundMovement != null) groundMovement.enabled = false;
        if (sensor != null) sensor.enabled = false;

        yield return new WaitForSecondsRealtime(hitFreezeDuration + 0.05f);
        
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }

    private void EnemyCombat_OnDamageReceived(object sender, EnemyCombat.DamageReceivedArgs e)
    {
        Quaternion spawnRotation = Quaternion.FromToRotation(Vector2.right, e.knockbackDir.normalized);
        Instantiate(hitParticlePrefab, transform.position, spawnRotation);
        colorFlasher.FlashColor(spriteRenderer, flashDuration, flashColor);
        TimeFreezer.Instance.FreezeTime(hitFreezeDuration);
    }
}
