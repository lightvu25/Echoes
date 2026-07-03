using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private MeleeAttack meleeAttack;
    private EnemyBrain enemyBrain;
    private Animator animator;

    private void Awake()
    {
        meleeAttack = GetComponentInParent<MeleeAttack>();
        enemyBrain = GetComponentInParent<EnemyBrain>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (enemyBrain != null)
        {
            enemyBrain.OnStateChanged += EnemyBrain_OnStateChanged;
            enemyBrain.OnAttack += EnemyBrain_OnAttack;
        }
        
        var health = GetComponentInParent<HealthSystem>();
        if (health != null)
        {
            health.OnDamaged += Health_OnDamaged;
            health.OnDeath += Health_OnDeath;
        }
    }

    private void OnDestroy()
    {
        if (enemyBrain != null)
        {
            enemyBrain.OnStateChanged -= EnemyBrain_OnStateChanged;
            enemyBrain.OnAttack -= EnemyBrain_OnAttack;
        }
        
        var health = GetComponentInParent<HealthSystem>();
        if (health != null)
        {
            health.OnDamaged -= Health_OnDamaged;
            health.OnDeath -= Health_OnDeath;
        }
    }

    private bool HasParameter(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    private void EnemyBrain_OnStateChanged(object sender, EnemyBrain.OnStateArgs e)
    {
        if (HasParameter("IsMoving"))
        {
            animator.SetBool("IsMoving", e.state == EnemyBrain.State.Chase || e.state == EnemyBrain.State.Patrol);
        }
    }

    private void EnemyBrain_OnAttack(object sender, System.EventArgs e)
    {
        if (HasParameter("Attack")) animator.SetTrigger("Attack");
    }

    private void Health_OnDamaged(object sender, HealthSystem.DamageEventArgs e)
    {
        if (HasParameter("Hit")) animator.SetTrigger("Hit");
    }

    private void Health_OnDeath(object sender, System.EventArgs e)
    {
        if (HasParameter("IsDead")) animator.SetBool("IsDead", true);
        if (HasParameter("Die")) animator.SetTrigger("Die");
    }

    public void TriggerHitbox()
    {        
        meleeAttack?.TriggerHitbox();
    }

    public void FinishAttack()
    { 
        meleeAttack?.FinishAttack();
    }
}