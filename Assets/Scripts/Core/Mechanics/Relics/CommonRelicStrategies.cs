using UnityEngine;

internal sealed class GraftingFlaskStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.OnEquip = () =>
        {
            c.Health?.SetMaxHPModifier(c.Source, 100, true);
            c.Modifiers?.SetMovementSpeed(c.Source, 0.95f);
        };
        b.OnUnequip = () =>
        {
            c.Health?.SetMaxHPModifier(c.Source, 0);
            c.Modifiers?.RemoveSource(c.Source);
        };
        return b;
    }
}

internal sealed class BloodthirstyMossStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.PickupCollected = () => { if (Random.value < 0.05f) c.Health?.Heal(1); };
        return b;
    }
}

internal sealed class AcidicGallbladderStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.IncomingDamagePriority = 0;
        b.IncomingDamage = (ref int damage, ref DamageInfo info) =>
        {
            if (info.damageSource == DamageSourceType.Poison ||
                info.damageSource == DamageSourceType.Environment_Thorns ||
                info.damageSource == DamageSourceType.ElectrifiedWater ||
                info.damageSource == DamageSourceType.FallDamage) damage = 0;
        };
        return b;
    }
}

internal sealed class DarkBandageStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.OnEquip = () => c.Health?.SetIFrameDurationBonus(c.Source, 0.5f);
        b.OnUnequip = () => c.Health?.SetIFrameDurationBonus(c.Source, 0f);
        return b;
    }
}

internal sealed class ShatteredMemoryStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        int stacks = 0;
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.EnemyKilled = _ =>
        {
            stacks = Mathf.Min(5, stacks + 1);
            c.Modifiers?.SetAttackSpeed(c.Source, 1f + stacks * 0.05f);
        };
        b.RoomEntered = _ => { stacks = 0; c.Modifiers?.SetAttackSpeed(c.Source, 1f); };
        b.OnUnequip = () => c.Modifiers?.RemoveSource(c.Source);
        return b;
    }
}

internal sealed class RustyGrappleStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.SuccessfulHit = hit =>
        {
            if (!RelicRuntimeContext.IsValid(hit?.target) || !hit.damageInfo.hasPlayerAttackMetadata ||
                hit.damageInfo.originatingPlaystyle != PlaystyleType.LongRange ||
                Vector2.Distance(c.Player.transform.position, hit.target.Transform.position) <= 2f) return;
            c.Movement?.PullToward(hit.target.Transform.position, 14f);
            hit.target.Transform.GetComponent<EchoStatusReceiver>()?.ApplyStun(0.5f);
        };
        return b;
    }
}

internal sealed class RottenWebStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.OnTick = () =>
        {
            if (c.Movement == null || c.Movement.isGrounded || !c.Movement.IsJumpHeld) return;
            float minimum = c.Attack != null && c.Attack.IsAttacking ? 0f : -1.5f;
            if (c.Movement.rb.linearVelocity.y < minimum)
                c.Movement.rb.linearVelocity = new Vector2(c.Movement.rb.linearVelocity.x, minimum);
        };
        return b;
    }
}

internal sealed class BurrowersScaleStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.IncomingDamage = (ref int damage, ref DamageInfo info) =>
        {
            if (info.damageSource == DamageSourceType.MeleeAttack)
                damage = Mathf.Max(0, Mathf.RoundToInt(damage * 0.85f));
        };
        return b;
    }
}

internal sealed class ToxicSporeStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.SuccessfulHit = hit =>
        {
            if (RelicRuntimeContext.IsValid(hit?.target) && hit.damageInfo.hasPlayerAttackMetadata &&
                hit.damageInfo.originatingAttackType == PlayerAttack.AttackType.Basic &&
                hit.damageInfo.originatingComboStep == 2)
                hit.target.Transform.GetComponent<EchoStatusReceiver>()?.ApplyPoison(4f, 3, c.Player);
        };
        return b;
    }
}
