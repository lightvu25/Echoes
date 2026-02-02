using System;
using UnityEngine;

public class EnemyVisual : MonoBehaviour
{
    private Animator animator;
    private EnemyMovement enemyMovement;
    private EnemyInteract enemyInteract;

    private GameObject currentIcon;

    [SerializeField] private GameObject noticeIconPrefab;
    [SerializeField] private Transform iconSpawnPoint;
    [SerializeField] private float noticeCooldown = 0.5f;

    private bool hasShownNotice = false;
    private float lastNoticeTime = -10f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyMovement = GetComponent<EnemyMovement>();
        enemyInteract = GetComponent<EnemyInteract>();
    }

    private void Start()
    {
        enemyMovement.OnPatrol += (s, e) => animator.SetBool("IsWalking", true);
        enemyMovement.OnIdle += (s, e) => animator.SetBool("IsWalking", false);

        enemyInteract.OnAttack += EnemyInteract_OnAttack;
        enemyInteract.OnNotice += EnemyInteract_OnNotice;
    }

    private void Update()
    {
        if (hasShownNotice)
        {
            if (enemyInteract.IsPlayerOutsideVision())
            {
                hasShownNotice = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (enemyMovement != null)
        {
            enemyMovement.OnPatrol -= EnemyMovement_OnPatrol;
        }
        if (enemyInteract != null)
        {
            enemyInteract.OnAttack -= EnemyInteract_OnAttack;
            enemyInteract.OnNotice -= EnemyInteract_OnNotice;
        }
    }

    private void EnemyMovement_OnPatrol(object sender, EventArgs e)
    {
        //animator.SetBool("IsRunning", true);
    }

    private void EnemyMovement_OnIdle(object sender, EventArgs e)
    {
        //animator.SetBool("IsRunning", false);
    }

    private void EnemyInteract_OnAttack(object sender, EventArgs e)
    {
        //animator.SetTrigger("Attack");
    }

    private void EnemyInteract_OnNotice(object sender, EventArgs e)
    {
        if (!hasShownNotice && Time.time >= lastNoticeTime + noticeCooldown)
        {
            currentIcon = Instantiate(noticeIconPrefab, iconSpawnPoint.position, Quaternion.identity, transform);
            
            Destroy(currentIcon, 1f);

            hasShownNotice = true;
            lastNoticeTime = Time.time;
        }
    }

    public void Die()
    {
        if (currentIcon != null)
        {
            Destroy(currentIcon);
        }
    }
}