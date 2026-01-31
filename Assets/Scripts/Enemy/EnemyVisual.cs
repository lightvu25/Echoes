using System;
using UnityEngine;

public class EnemyVisual : MonoBehaviour
{
    private Animator animator;
    private EnemyMovement enemyMovement;
    private EnemyInteract enemyInteract;

    [SerializeField] private GameObject noticeIconPrefab;
    [SerializeField] private Transform iconSpawnPoint;

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

    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi memory leak
        if (enemyMovement != null)
        {
            enemyMovement.OnPatrol -= EnemyMovement_OnPatrol;
            enemyMovement.OnIdle -= EnemyMovement_OnIdle;
        }
        if (enemyInteract != null)
        {
            enemyInteract.OnAttack -= EnemyInteract_OnAttack;
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
        //animator.SetTrigger("Notice");
        //Instantiate(noticeIconPrefab, iconSpawnPoint.position, Quaternion.identity, transform);
    }
}