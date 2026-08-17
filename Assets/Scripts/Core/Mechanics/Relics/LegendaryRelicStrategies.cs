using System.Collections;
using UnityEngine;

internal sealed class PassiveRelicStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c) => new RelicRuntimeBehavior(c);
}

internal sealed class OreSparkCoreStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        int attacks = 0;
        int chargedAttackSequence = 0;
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.AttackStarted = (_, args) =>
        {
            if (args.attackType != PlayerAttack.AttackType.Basic) return;
            attacks++;
            if (attacks < 3) return;
            attacks = 0;
            chargedAttackSequence = args.attackSequenceId;
        };
        b.SuccessfulHit = hit =>
        {
            if (chargedAttackSequence == 0 || !RelicRuntimeContext.IsValid(hit?.target) ||
                hit.damageInfo.attackSequenceId != chargedAttackSequence) return;
            chargedAttackSequence = 0;
            c.ChainDamage(hit.target, Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.5f)), 3);
        };
        return b;
    }
}

internal sealed class StalactiteHeartStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.FatalDamage = (ref bool preventDeath) =>
        {
            if (preventDeath) return;
            preventDeath = true;
            c.Health?.Heal(Mathf.CeilToInt(c.Health.MaxHP * 0.5f));
            foreach (IDamageable target in RelicRuntimeContext.FindTargets(c.Player.transform.position, 10f))
                target.Transform.GetComponent<EchoStatusReceiver>()?.ApplyFreeze(3f);
            PlayerInventoryCore.Instance?.RemoveItemByID("StalactiteHeart");
        };
        return b;
    }
}

internal sealed class SoulBellStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        bool shield = false;
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.IncomingDamagePriority = 200;
        b.EnemyKilled = kill => { if (kill.IsEliteOrBoss) shield = true; };
        b.IncomingDamage = (ref int damage, ref DamageInfo _) =>
        {
            if (!shield || damage <= 0) return;
            shield = false;
            damage = 0;
        };
        return b;
    }
}

internal sealed class CondemnedRingStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.OnEquip = () => c.Health?.SetMaxHPCap(c.Source, 1);
        b.OnUnequip = () => c.Health?.SetMaxHPCap(c.Source, 0);
        b.OutgoingDamage = (IDamageable _, ref DamageInfo info) => c.ForceCritical(ref info);
        return b;
    }
}

internal sealed class EchoingSigilStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        IEnumerator Echo(IDamageable target, int originalDamage)
        {
            yield return new WaitForSeconds(0.25f);
            if (!RelicRuntimeContext.IsValid(target) || target.IsDead) yield break;
            DamageInfo echo = DamageInfo.Create(Mathf.Max(1, Mathf.RoundToInt(originalDamage * 0.6f)), c.Player);
            echo.damageSource = DamageSourceType.RelicSecondary;
            echo.isTrueDamage = true;
            target.TakeDamage(echo);
        }
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.SuccessfulHit = hit =>
        {
            if (RelicRuntimeContext.IsValid(hit?.target))
                c.Host.StartBehaviorCoroutine(Echo(hit.target, hit.finalDamage));
        };
        return b;
    }
}

internal sealed class AbyssalTreadsStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.OnEquip = () => c.Movement?.SetTripleJump(c.Source, true);
        b.OnUnequip = () => c.Movement?.SetTripleJump(c.Source, false);
        b.JumpPerformed = ordinal =>
        {
            if (ordinal != 3) return;
            foreach (IDamageable target in RelicRuntimeContext.FindTargets(c.Player.transform.position, 4f))
            {
                if (!(target is EnemyCombat enemy)) continue;
                Vector2 direction = target.Transform.position - c.Player.transform.position;
                if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
                enemy.ApplyExternalKnockback(direction.normalized, 12f);
            }
        };
        return b;
    }
}

internal sealed class VampiricFangStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.EnemyKilled = kill =>
        {
            if (kill.KillingBlow.damageSource == DamageSourceType.PlungeAttack ||
                kill.KillingBlow.damageSource == DamageSourceType.PlungeFall)
                c.Health?.Heal(Mathf.CeilToInt(c.Health.MaxHP * 0.1f));
        };
        return b;
    }
}
