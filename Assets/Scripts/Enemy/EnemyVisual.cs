using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class EnemyVisual : MonoBehaviour, IFeedbackProvider
{
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

    private void Start()
    {
        if (groundMovement != null)
        {
            groundMovement.OnPatrol += Movement_OnPatrol;
            groundMovement.OnIdle += Movement_OnIdle;
        }

        if (brain != null)
        {
            brain.OnAttack += Brain_OnAttack;
            brain.OnNotice += Brain_OnNotice;
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
        }
        if (enemyCombat != null)
        {
            enemyCombat.OnEnemyDied -= EnemyCombat_OnEnemyDied;
            enemyCombat.OnDamageReceived -= EnemyCombat_OnDamageReceived;
        }
    }

    private void Movement_OnPatrol(object sender, EventArgs e)
    {
        animator.SetBool("isRunning", false);
        animator.SetBool("isPatroling", true);
    }

    private void Movement_OnIdle(object sender, EventArgs e)
    {
        animator.SetBool("isRunning", false);
        animator.SetBool("isPatroling", false);
    }

    private void Brain_OnAttack(object sender, EventArgs e)
    {
        animator.SetTrigger("isAttacking");
        animator.SetBool("isPatroling", false);
        animator.SetBool("isRunning", false);
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
        
        Destroy(gameObject);
    }

    private void EnemyCombat_OnDamageReceived(object sender, EnemyCombat.DamageReceivedArgs e)
    {
        Quaternion spawnRotation = Quaternion.FromToRotation(Vector2.right, e.knockbackDir.normalized);
        Instantiate(hitParticlePrefab, transform.position, spawnRotation);
        colorFlasher.FlashColor(spriteRenderer, flashDuration, flashColor);
        TimeFreezer.Instance.FreezeTime(hitFreezeDuration);
    }
}
