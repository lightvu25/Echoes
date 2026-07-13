using System;
using UnityEngine;

public class BomberDualAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;

    [SerializeField] private float nearAttackRange = 3f;
    [SerializeField] private float lowHpThreshold = 0.3f;
    [SerializeField] private MonoBehaviour farAttackScript;
    [SerializeField] private MonoBehaviour nearAttackScript;

    private IEnemyAttack farAttack;
    private IEnemyAttack nearAttack;
    private IEnemyAttack activeAttack;
    
    private EnemySensor sensor;
    private EnemyCombat combat;
    private EnemyBrain brain;

    public bool IsAttacking => activeAttack != null && activeAttack.IsAttacking;

    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();
        combat = GetComponent<EnemyCombat>();
        brain = GetComponent<EnemyBrain>();
        
        farAttack = farAttackScript as IEnemyAttack;
        nearAttack = nearAttackScript as IEnemyAttack;

        if (farAttack == null) Debug.LogWarning("[BomberDualAttack] farAttackScript does not implement IEnemyAttack!");
        if (nearAttack == null) Debug.LogWarning("[BomberDualAttack] nearAttackScript does not implement IEnemyAttack!");

        if (farAttack != null)
        {
            farAttack.OnAttackStarted += (s, e) => OnAttackStarted?.Invoke(this, e);
            farAttack.OnAttackFinished += (s, e) => { activeAttack = null; OnAttackFinished?.Invoke(this, e); };
        }

        if (nearAttack != null)
        {
            nearAttack.OnAttackStarted += (s, e) => OnAttackStarted?.Invoke(this, e);
            nearAttack.OnAttackFinished += (s, e) => { activeAttack = null; OnAttackFinished?.Invoke(this, e); };
        }
    }

    private void Update()
    {
        if (combat != null && combat.HPPercent <= lowHpThreshold)
        {
            if (brain != null) brain.CanBackstep = false;
        }
    }

    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        if (sensor == null || farAttack == null || nearAttack == null) return;

        if (combat != null && combat.HPPercent <= lowHpThreshold && sensor.DistanceToPlayer <= nearAttackRange)
        {
            activeAttack = nearAttack;
        }
        else
        {
            activeAttack = farAttack;
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
    }
}
