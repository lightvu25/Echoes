using System;
using UnityEngine;

public class EnemyVisual : MonoBehaviour
{
    private Animator animator;
    private EnemyMovement enemyMovement;
    private EnemyInteract enemyInteract;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyMovement = GetComponent<EnemyMovement>();
        enemyInteract = GetComponent<EnemyInteract>();
    }

    private void Start()
    {
        // Đăng ký sự kiện
        enemyMovement.OnStartMoving += EnemyMovement_OnStartMoving;
        enemyMovement.OnStopMoving += EnemyMovement_OnStopMoving;
        enemyInteract.OnAttack += EnemyInteract_OnAttack;
    }

    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi memory leak
        if (enemyMovement != null)
        {
            enemyMovement.OnStartMoving -= EnemyMovement_OnStartMoving;
            enemyMovement.OnStopMoving -= EnemyMovement_OnStopMoving;
        }
        if (enemyInteract != null)
        {
            enemyInteract.OnAttack -= EnemyInteract_OnAttack;
        }
    }

    private void EnemyMovement_OnStartMoving(object sender, EventArgs e)
    {
        animator.SetBool("IsRunning", true);
    }

    private void EnemyMovement_OnStopMoving(object sender, EventArgs e)
    {
        animator.SetBool("IsRunning", false);
    }

    private void EnemyInteract_OnAttack(object sender, EventArgs e)
    {
        animator.SetTrigger("Attack");
    }
}