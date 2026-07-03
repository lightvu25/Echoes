using System;
using UnityEngine;

/// <summary>
/// A wrapper attack script that delegates to two different attacks based on distance to the player.
/// Put this on your main Enemy GameObject, and put your two actual attacks (ThrowBombAttack, BombAttack)
/// on child GameObjects so the EnemyBrain doesn't get confused about which IEnemyAttack to grab.
/// </summary>
public class BomberDualAttack : MonoBehaviour, IEnemyAttack
{
    public event EventHandler OnAttackStarted;
    public event EventHandler OnAttackFinished;

    [Header("Distance Settings")]
    [Tooltip("If the player is closer than this, the near attack is used. Otherwise, far attack is used.")]
    [SerializeField] private float nearAttackRange = 3f;

    [Header("Sub-Attacks (Place on Child GameObjects)")]
    [Tooltip("The script (implementing IEnemyAttack) to trigger when the player is far. e.g., ThrowBombAttack")]
    [SerializeField] private MonoBehaviour farAttackScript;

    [Tooltip("The script (implementing IEnemyAttack) to trigger when the player is near. e.g., BombAttack")]
    [SerializeField] private MonoBehaviour nearAttackScript;

    private IEnemyAttack farAttack;
    private IEnemyAttack nearAttack;
    private IEnemyAttack activeAttack;
    
    private EnemySensor sensor;

    public bool IsAttacking => activeAttack != null && activeAttack.IsAttacking;

    private void Awake()
    {
        sensor = GetComponent<EnemySensor>();
        
        farAttack = farAttackScript as IEnemyAttack;
        nearAttack = nearAttackScript as IEnemyAttack;

        if (farAttack == null) Debug.LogWarning("[BomberDualAttack] farAttackScript does not implement IEnemyAttack!");
        if (nearAttack == null) Debug.LogWarning("[BomberDualAttack] nearAttackScript does not implement IEnemyAttack!");

        // Forward the events from the sub-attacks up to the EnemyBrain
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

    public void ExecuteAttack()
    {
        if (IsAttacking) return;
        if (sensor == null || farAttack == null || nearAttack == null) return;

        if (sensor.DistanceToPlayer <= nearAttackRange)
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
