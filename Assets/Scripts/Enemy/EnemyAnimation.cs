using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private EnemyAttack enemyAttack;
    private EnemyInteract enemyInteract;

    private void Awake()
    {
        enemyAttack = GetComponentInParent<EnemyAttack>();
        enemyInteract = GetComponentInParent<EnemyInteract>();
    }

    public void TriggerHitbox()
    {        
        enemyAttack.TriggerHitbox();
    }

    public void FinishAttack()
    { 
        enemyInteract.FinishAttack();
    }
}
