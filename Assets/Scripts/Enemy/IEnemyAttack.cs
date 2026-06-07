using System;

public interface IEnemyAttack
{
    event EventHandler OnAttackStarted;
    event EventHandler OnAttackFinished;

    bool IsAttacking { get; }
    void ExecuteAttack();
    void CancelAttack();
}
