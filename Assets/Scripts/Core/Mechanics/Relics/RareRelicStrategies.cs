using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class RustedCoinStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.OutgoingDamage = (IDamageable _, ref DamageInfo info) =>
        {
            int gold = PlayerStats.Instance != null ? PlayerStats.Instance.CurrentGold : 0;
            info.multiplicativeStack *= 1f + Mathf.Min(0.25f, (gold / 100) * 0.01f);
        };
        return b;
    }
}

internal sealed class EmberFireflyStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        List<RelicBurnZone> zones = new List<RelicBurnZone>();
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.PlungeImpact = impact =>
        {
            GameObject zoneObject = new GameObject("Ember Firefly Burn Zone");
            zoneObject.transform.position = impact.Center;
            RelicBurnZone zone = zoneObject.AddComponent<RelicBurnZone>();
            zone.Initialize(c.Player, 3f, 2.25f, Mathf.Max(2, Mathf.RoundToInt(impact.Damage * 0.15f)),
                expired => zones.Remove(expired));
            zones.Add(zone);
        };
        b.OnUnequip = () =>
        {
            foreach (RelicBurnZone zone in zones)
                if (zone != null) Object.Destroy(zone.gameObject);
            zones.Clear();
        };
        return b;
    }
}

internal sealed class SpikedCleatsStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        Coroutine active = null;
        IEnumerator Boost()
        {
            c.Modifiers?.SetMovementSpeed(c.Source, 1.2f);
            yield return new WaitForSeconds(1.5f);
            c.Modifiers?.SetMovementSpeed(c.Source, 1f);
            active = null;
        }
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.WallJump = (_, __) =>
        {
            if (active != null) c.Host.StopBehaviorCoroutine(active);
            active = c.Host.StartBehaviorCoroutine(Boost());
        };
        b.OnUnequip = () => c.Modifiers?.RemoveSource(c.Source);
        return b;
    }
}

internal sealed class EchoWhetstoneStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        bool armed = false;
        float expires = 0f;
        int boostedAttackSequence = 0;
        IEnumerator ScanDash()
        {
            Vector2 start = c.Player.transform.position;
            while (c.Movement != null && c.Movement.isDashing) yield return null;
            Vector2 end = c.Player.transform.position;
            Vector2 center = (start + end) * 0.5f;
            Vector2 size = new Vector2(Mathf.Abs(end.x - start.x) + 1.5f, Mathf.Abs(end.y - start.y) + 2f);
            if (Physics2D.OverlapBoxAll(center, size, 0f, RelicRuntimeContext.EnemyMask).Length > 0)
            {
                armed = true;
                expires = Time.time + 2f;
            }
        }
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.Dash = (_, __) => c.Host.StartBehaviorCoroutine(ScanDash());
        b.AttackStarted = (_, args) =>
        {
            if (!armed) return;
            if (Time.time > expires) { armed = false; return; }
            boostedAttackSequence = args.attackSequenceId;
            armed = false;
        };
        b.OutgoingDamage = (IDamageable _, ref DamageInfo info) =>
        {
            if (boostedAttackSequence != 0 && info.attackSequenceId == boostedAttackSequence)
                info.multiplicativeStack *= 2f;
        };
        b.OnTick = () => { if (armed && Time.time > expires) armed = false; };
        return b;
    }
}

internal sealed class BouncerShroomStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.SuccessfulHit = _ =>
        {
            if (c.Movement != null && !c.Movement.isGrounded && c.Movement.rb.linearVelocity.y < 0f &&
                c.Movement.LastPressedJumpTime > 0f) c.Movement.ApplyRelicBounce(13f);
        };
        return b;
    }
}

internal sealed class VialOfAshesStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.EnemyKilled = kill =>
        {
            if (kill.Enemy == null) return;
            EchoStatusReceiver status = kill.Enemy.GetComponent<EchoStatusReceiver>();
            if (status != null && (status.IsBurning || status.IsPoisoned))
                c.DealAreaDamage(kill.Enemy.transform.position, 3.5f, 20, kill.Enemy, 8);
        };
        return b;
    }
}

internal sealed class BatsTalonStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.OutgoingDamage = (IDamageable _, ref DamageInfo info) =>
        {
            if (info.hasPlayerAttackMetadata && info.originatedInAir)
                info.multiplicativeStack *= 1.3f;
        };
        return b;
    }
}

internal sealed class RustyHeavyChainStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.OnEquip = () => c.Attack?.SetPlungeModifiers(c.Source, 1.5f, 1.5f);
        b.OnUnequip = () => c.Attack?.RemovePlungeModifiers(c.Source);
        return b;
    }
}

internal sealed class DriedCyclopsEyeStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        float expires = -1f;
        HashSet<EnemyVisual> highlighted = new HashSet<EnemyVisual>();
        void ClearHighlights()
        {
            foreach (EnemyVisual visual in highlighted)
                if (visual != null) visual.SetWeakPointHighlighted(c.Source, false);
            highlighted.Clear();
        }

        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.RoomEntered = room =>
        {
            ClearHighlights();
            expires = Time.time + 5f;
            Bounds bounds = room.GetBounds();
            foreach (EnemyCombat enemy in Object.FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None))
            {
                if (enemy == null || enemy.IsDead || !bounds.Contains(enemy.transform.position)) continue;
                EnemyVisual visual = enemy.GetComponent<EnemyVisual>();
                if (visual == null || !highlighted.Add(visual)) continue;
                visual.SetWeakPointHighlighted(c.Source, true);
            }
        };
        b.OutgoingDamage = (IDamageable _, ref DamageInfo info) =>
        {
            if (Time.time <= expires) c.ForceCritical(ref info);
        };
        b.OnTick = () =>
        {
            if (expires < 0f || Time.time <= expires) return;
            expires = -1f;
            ClearHighlights();
        };
        b.OnUnequip = ClearHighlights;
        return b;
    }
}

internal sealed class VolatileCoreStrategy : IRelicRuntimeStrategy
{
    public RelicRuntimeBehavior Create(RelicRuntimeContext c)
    {
        bool reflecting = false;
        RelicRuntimeBehavior b = new RelicRuntimeBehavior(c);
        b.IncomingDamage = (ref int damage, ref DamageInfo info) =>
        {
            if (info.damageSource != DamageSourceType.BombAttack) return;
            info.knockbackForce = 0f;
            info.knockbackDirection = Vector2.zero;
            info.suppressHitReaction = true;
            if (reflecting || damage <= 0) return;
            reflecting = true;
            c.DealAreaDamage(c.Player.transform.position, 4f,
                Mathf.Max(1, Mathf.RoundToInt(damage * 0.5f)), null, 6);
            reflecting = false;
        };
        return b;
    }
}
