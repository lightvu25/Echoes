using System;
using UnityEngine;

public class MeleeDualAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;

    [SerializeField] private MonoBehaviour meleeAttackScript;
    [SerializeField] private MonoBehaviour dashAttackScript;
    
    [Header("Dash Attack Triggers")]
    [SerializeField] private float dashAttackRange = 5f;
    [SerializeField] private float forceBackstepCooldown = 2f;

    private IEnemyAttack meleeAttack;
    private IEnemyAttack dashAttack;
    private IEnemyAttack activeAttack;

    public IEnemyAttack MeleeAttack => meleeAttack;
    public IEnemyAttack DashAttack => dashAttack;
    public IEnemyAttack ActiveAttack => activeAttack;
    
    private EnemySensor sensor;
    private EnemyBrain brain;
    
    private float lastForcedDashTime = -999f;
    private bool dashAttackQueued = false;

    public bool IsAttacking => activeAttack != null && activeAttack.IsAttacking;

    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();
        brain = GetComponent<EnemyBrain>();
        
        meleeAttack = meleeAttackScript as IEnemyAttack;
        dashAttack = dashAttackScript as IEnemyAttack;

        if (meleeAttack != null)
        {
            meleeAttack.OnAttackStarted += (s, e) => OnAttackStarted?.Invoke(this, e);
            meleeAttack.OnAttackFinished += (s, e) => { activeAttack = null; OnAttackFinished?.Invoke(this, e); };
        }

        if (dashAttack != null)
        {
            dashAttack.OnAttackStarted += (s, e) => OnAttackStarted?.Invoke(this, e);
            dashAttack.OnAttackFinished += (s, e) => { activeAttack = null; OnAttackFinished?.Invoke(this, e); };
        }
    }

    private void Update()
    {
        if (brain == null || sensor == null || sensor.TargetPlayer == null) return;
        
        // Do not interrupt if already attacking or backstepping
        if (IsAttacking || brain.CurrentState == EnemyBrain.State.Backstep || brain.CurrentState == EnemyBrain.State.Attack) return;
        
        // Respect cooldown
        if (Time.time < lastForcedDashTime + forceBackstepCooldown) return;

        PlayerAttack pAttack = sensor.TargetPlayer.GetComponentInParent<PlayerAttack>();
        bool playerAttacking = pAttack != null && pAttack.IsAttacking;
        float dist = sensor.DistanceToPlayer;
        
        bool shouldDash = false;
        bool shouldBackstepFirst = false;

        if (playerAttacking)
        {
            if (dist <= 3f) 
            {
                // Player attacking close to enemy -> Step back and perform dash attack
                shouldDash = true;
                shouldBackstepFirst = true;
            }
            else if (dist <= dashAttackRange)
            {
                // Player attacking far from enemy -> Start perform dash attack
                shouldDash = true;
                shouldBackstepFirst = false;
            }
        }
        else if (dist <= 3f && brain.CurrentState == EnemyBrain.State.Chase)
        {
            // Player runs into enemy (ran into the attack range) -> Step back and perform dash attack
            shouldDash = true;
            shouldBackstepFirst = true;
        }

        if (shouldDash && brain.CanBackstep)
        {
            lastForcedDashTime = Time.time;
            dashAttackQueued = true;
            
            if (shouldBackstepFirst)
            {
                brain.ForceBackstep();
            }
        }
    }

    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        if (meleeAttack == null || dashAttack == null) return;

        if (dashAttackQueued)
        {
            activeAttack = dashAttack;
            dashAttackQueued = false;
        }
        else
        {
            activeAttack = meleeAttack;
        }

        activeAttack.ExecuteAttack();
    }

    public void CancelAttack()
    {
        if (activeAttack != null)
        {
            activeAttack.CancelAttack();
            activeAttack = null;
        }
        dashAttackQueued = false;
    }
}
