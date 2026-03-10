using System;
using UnityEngine;

public class EnemyVisual : MonoBehaviour
{
    private EnemyMovement enemyMovement;
    private EnemyInteract enemyInteract;
    private EnemyCombat enemyCombat;

    [SerializeField] private GameObject noticeIconPrefab;
    [SerializeField] private Transform iconSpawnPoint;
    [SerializeField] private float noticeCooldown;
    [SerializeField] private Animator animator;

    private GameObject currentIcon;
    private bool hasShownNotice = false;
    private float lastNoticeTime = -10f;

    private void Awake()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        enemyInteract = GetComponent<EnemyInteract>();
        enemyCombat = GetComponent<EnemyCombat>();
    }

    private void Start()
    {
        enemyMovement.OnPatrol += EnemyMovement_OnPatrol;
        enemyMovement.OnIdle += EnemyMovement_OnIdle;

        enemyInteract.OnAttack += EnemyInteract_OnAttack;
        enemyInteract.OnNotice += EnemyInteract_OnNotice;

        if (enemyCombat != null)
        {
            enemyCombat.OnEnemyDied += EnemyCombat_OnEnemyDied;
        }
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
            enemyMovement.OnIdle -= EnemyMovement_OnIdle;
        }
        if (enemyInteract != null)
        {
            enemyInteract.OnAttack -= EnemyInteract_OnAttack;
            enemyInteract.OnNotice -= EnemyInteract_OnNotice;
        }
        if (enemyCombat != null)
        {
            enemyCombat.OnEnemyDied -= EnemyCombat_OnEnemyDied;
        }
    }

    private void EnemyMovement_OnPatrol(object sender, EventArgs e)
    {
        animator.SetBool("isRunning", false);
        animator.SetBool("isPatroling", true);
    }

    private void EnemyMovement_OnIdle(object sender, EventArgs e)
    {
        animator.SetBool("isRunning", false);
        animator.SetBool("isPatroling", false);
    }

    private void EnemyInteract_OnAttack(object sender, EventArgs e)
    {
        animator.SetTrigger("isAttacking");
        animator.SetBool("isPatroling", false);
        animator.SetBool("isRunning", false);
    }

    private void EnemyInteract_OnNotice(object sender, EventArgs e)
    {
        if (!hasShownNotice && Time.time >= lastNoticeTime + noticeCooldown)
        {
            currentIcon = Instantiate(noticeIconPrefab, iconSpawnPoint.position, Quaternion.identity, transform);
            Destroy(currentIcon, 4f);

            hasShownNotice = true;
            lastNoticeTime = Time.time;
        }
    }

    private void EnemyCombat_OnEnemyDied(object sender, EventArgs e)
    {
        Die();
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}