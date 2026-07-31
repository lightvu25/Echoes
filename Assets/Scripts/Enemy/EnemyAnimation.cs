using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private MeleeAttack meleeAttack;
    private EnemyBrain enemyBrain;
    private Animator animator;
    private EntityAudioManager audioManager;

    private void Awake()
    {
        enemyBrain = GetComponentInParent<EnemyBrain>();
        meleeAttack = enemyBrain != null
            ? enemyBrain.GetComponentInChildren<MeleeAttack>(true)
            : GetComponentInParent<MeleeAttack>();
        animator = GetComponent<Animator>();
        audioManager = GetComponentInParent<EntityAudioManager>();
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
        bool isMoving = e.state == EnemyBrain.State.Chase || e.state == EnemyBrain.State.Patrol;

        if (HasParameter("IsMoving"))
        {
            animator.SetBool("IsMoving", isMoving);
        }

        if (audioManager != null)
        {
            if (isMoving)
            {
                if (e.state == EnemyBrain.State.Chase)
                {
                    audioManager.PlayLoopingSound("Chase");
                }
                else if (e.state == EnemyBrain.State.Patrol)
                {
                    audioManager.PlayLoopingSound("Patrol");
                }
            }
            else
            {
                audioManager.StopLoopingSound();
            }

            if (e.state == EnemyBrain.State.Telegraph)
            {
                audioManager.PlaySound("Roar");
            }
        }
    }

    private void EnemyBrain_OnAttack(object sender, System.EventArgs e)
    {
        var dualAttack = enemyBrain.GetComponent<MeleeDualAttack>();
        if (dualAttack != null)
        {
            if (dualAttack.ActiveAttack == dualAttack.DashAttack)
            {
                if (HasParameter("DashAttack")) animator.SetTrigger("DashAttack");
                else if (HasParameter("Attack")) animator.SetTrigger("Attack");
            }
            else
            {
                if (HasParameter("MeleeAttack")) animator.SetTrigger("MeleeAttack");
                else if (HasParameter("Attack")) animator.SetTrigger("Attack");
            }
        }
        else
        {
            if (HasParameter("Attack")) animator.SetTrigger("Attack");
        }
    }

    private void Health_OnDamaged(object sender, HealthSystem.DamageEventArgs e)
    {
        if (HasParameter("Hit")) animator.SetTrigger("Hit");
    }

    private void Health_OnDeath(object sender, System.EventArgs e)
    {
        if (HasParameter("IsDead")) animator.SetBool("IsDead", true);
        if (HasParameter("Die")) animator.SetTrigger("Die");

        if (audioManager != null)
        {
            audioManager.StopLoopingSound();
            audioManager.PlaySoundGlobal("Dead");
        }
    }

    public void TriggerHitbox()
    {        
        var dualAttack = enemyBrain != null ? enemyBrain.GetComponent<MeleeDualAttack>() : null;
        if (dualAttack != null && dualAttack.ActiveAttack == dualAttack.DashAttack)
        {
            // Dash Attack handles its own damage loop, ignore the animation event to prevent double-hitting!
            return;
        }

        meleeAttack?.TriggerHitbox();
    }

    public void FinishAttack()
    { 
        var dualAttack = enemyBrain != null ? enemyBrain.GetComponent<MeleeDualAttack>() : null;
        if (dualAttack != null && dualAttack.ActiveAttack == dualAttack.DashAttack)
        {
            // Dash Attack finishes on its own timer, ignore the animation event!
            return;
        }

        meleeAttack?.FinishAttack();
    }

    // Call this from Animation Events to play sounds like "Roar", "Patrol", "Chase"
    public void PlaySound(string soundId)
    {
        if (audioManager != null)
        {
            audioManager.PlaySound(soundId);
        }
    }
}
