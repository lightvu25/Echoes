using System.Collections.Generic;
using UnityEngine;

public abstract class FusionHitModifier : IEchoModifier
{
    protected EchoModifierContext Context;
    protected abstract string ModifierId { get; }
    public virtual int Priority => 200;

    public virtual void Initialize(EchoModifierContext context)
    {
        Context = context;
        if (Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnHitTarget += HandleHit;
    }

    public virtual void Remove()
    {
        if (Context != null && Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnHitTarget -= HandleHit;
    }

    private void HandleHit(object sender, AttackHitbox.HitEventArgs hit)
    {
        if (FusionCombatUtility.IsFusionHit(hit, ModifierId)) OnFusionHit(hit);
    }

    protected abstract void OnFusionHit(AttackHitbox.HitEventArgs hit);
}

public class PlasmaModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_PLASMA";
    private FusionRuntimeHost host;

    public override void Initialize(EchoModifierContext context)
    {
        base.Initialize(context);
        host = context.PlayerGameObject.GetComponent<FusionRuntimeHost>();
    }

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        int damage = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.45f));
        if (host != null)
            host.StartPlasmaChain(hit.target, damage, Context, hit.damageInfo.activeEcho.chainLightningVFXPrefab);
    }

    public override void Remove()
    {
        if (host != null) host.StopAllFields();
        base.Remove();
    }
}

public class AvalancheModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_AVALANCHE";
    private readonly Dictionary<IDamageable, int> fracture = new Dictionary<IDamageable, int>();

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        EchoStatusReceiver status = FusionCombatUtility.GetStatus(hit.target);
        if (status == null) return;
        
        bool frozen = status.IsFrozen;
        if (!frozen) status.ApplySlow(3f);
        
        int stacks = frozen ? 3 : (fracture.TryGetValue(hit.target, out int current) ? current + 1 : 1);
        if (stacks < 3) { fracture[hit.target] = stacks; return; }
        
        fracture.Remove(hit.target);
        if (frozen) status.ForceRemoveFreeze();
        
        int damage = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 1.8f));
        FusionCombatUtility.DealDamage(hit.target, damage, Context);
        FusionCombatUtility.DealArea(hit.target.Transform.position, 2.5f, damage, Context, hit.target);
        
        GameObject explosionVfx = Context.EchoExplosionPrefab;
        FusionCombatUtility.PlayExplosionFeedback(hit.target.Transform.position, explosionVfx);
    }

    public override void Remove() { base.Remove(); fracture.Clear(); }
}

public class AfterburnerModifier : FusionHitModifier
{
    private const float BurningLineLength = 6f;
    private const float BurningLineWidth = 1.25f;
    private const float BurningLineDuration = 4f;
    private const float BurningLineOffsetY = -0.5f;

    protected override string ModifierId => "FUS_AFTERBURNER";
    private int hitCount;
    private FusionRuntimeHost host;

    public override void Initialize(EchoModifierContext context)
    {
        base.Initialize(context);
        host = context.PlayerGameObject.GetComponent<FusionRuntimeHost>();
    }

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        if (++hitCount % 3 != 0) return;
        int bonus = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.75f));
        FusionCombatUtility.DealDamage(hit.target, bonus, Context);

        if (host != null && FusionCombatUtility.TryCreateGroundLine(
                hit.target.Transform.position, BurningLineLength, out Vector2 start, out Vector2 end, BurningLineOffsetY))
        {
            host.StartBurningLine(start, end, BurningLineWidth, BurningLineDuration,
                Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.15f)), Context, Context.FireTrailPrefab);
        }
    }

    public override void Remove()
    {
        if (host != null) host.StopAllFields();
        base.Remove();
    }
}

public class EntropyModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_ENTROPY";
    private float lastRoll = 1f;

    public override void Initialize(EchoModifierContext context)
    {
        base.Initialize(context);
        if (Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied += BeforeDamage;
    }

    public override void Remove()
    {
        if (Context != null && Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied -= BeforeDamage;
        base.Remove();
    }

    private void BeforeDamage(IDamageable target, ref DamageInfo damage)
    {
        if (damage.activeEcho == null || damage.activeEcho.uniqueModifierID != ModifierId) return;
        lastRoll = Random.Range(0.6f, 2f);
        damage.multiplicativeStack *= lastRoll;
        damage.isTrueDamage = true;
    }

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        if (lastRoll < 1.0f && Context.PlayerHealth != null)
        {
            DamageInfo recoil = DamageInfo.Create(Mathf.Max(1, Mathf.RoundToInt(Context.PlayerHealth.MaxHP * 0.02f)), Context.PlayerGameObject);
            recoil.damageSource = DamageSourceType.FusionRecoil;
            recoil.isTrueDamage = true;
            Context.PlayerHealth.TakeDamage(recoil);
        }
        else if (lastRoll >= 1.0f)
        {
            FusionCombatUtility.DealArea(hit.target.Transform.position, 2.5f,
                Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.6f)), Context, hit.target, true);
                
            GameObject explosionVfx = Context.EchoExplosionPrefab;
            FusionCombatUtility.PlayExplosionFeedback(hit.target.Transform.position, explosionVfx);
        }
    }
}

public class OverclockModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_OVERCLOCK";
    private int stacks;
    private float lastHitTime;

    private FusionRuntimeHost host;

    public override void Initialize(EchoModifierContext context)
    {
        base.Initialize(context);
        host = context.PlayerGameObject.GetComponent<FusionRuntimeHost>();
        if (Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied += BeforeDamage;
    }

    public override void Remove()
    {
        if (Context != null && Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied -= BeforeDamage;
        base.Remove();
    }

    private void BeforeDamage(IDamageable target, ref DamageInfo damage)
    {
        if (damage.activeEcho == null || damage.activeEcho.uniqueModifierID != ModifierId) return;
        if (Time.time - lastHitTime > 2f) stacks = 0;
        damage.linearModifierSum += stacks * 0.12f;
    }

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        if (Time.time - lastHitTime > 2f) stacks = 0;
        lastHitTime = Time.time;
        stacks++;
        if (stacks < 5) return;
        stacks = 0;
        
        int damage = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 2.5f));
        if (host != null && hit.damageInfo.activeEcho != null)
        {
            host.StartPlasmaChain(hit.target, damage, Context, hit.damageInfo.activeEcho.chainLightningVFXPrefab);
        }
        else
        {
            FusionCombatUtility.DealArea(hit.target.Transform.position, 4f, damage, Context, hit.target);
        }
        
        if (Context.PlayerHealth != null)
        {
            DamageInfo recoil = DamageInfo.Create(Mathf.Max(1, Mathf.RoundToInt(Context.PlayerHealth.MaxHP * 0.05f)), Context.PlayerGameObject);
            recoil.damageSource = DamageSourceType.FusionRecoil;
            recoil.isTrueDamage = true;
            Context.PlayerHealth.TakeDamage(recoil);
        }
    }
}

public class NeonGridModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_NEON_GRID";
    private readonly List<FusionGlitchNode> nodes = new List<FusionGlitchNode>();

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        nodes.RemoveAll(node => node == null || node.IsDead);
        Vector2 impact = hit.target.Transform.position;
        
        GameObject nodeObject = FusionCombatUtility.SpawnVfx(Context.GlitchedZonePrefab, impact, 5f);
        if (nodeObject == null) return;
        
        FusionGlitchNode node = nodeObject.GetComponent<FusionGlitchNode>();
        if (node == null) node = nodeObject.AddComponent<FusionGlitchNode>();
        
        int nodeDamage = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 1.2f));
        int pulseDamage = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.3f));
        
        node.Configure(triggered => DetonateLinkedNodes(triggered, nodeDamage),
            pulsing => PulseNode(pulsing, pulseDamage));
        nodes.Add(node);
    }

    private void PulseNode(FusionGlitchNode node, int damage)
    {
        if (node == null || node.IsDead) return;
        FusionCombatUtility.DealArea(node.transform.position, 1.75f, damage, Context);
    }

    private void DetonateLinkedNodes(FusionGlitchNode triggered, int damage)
    {
        Vector2 origin = triggered.transform.position;
        GameObject explosionVfx = Context.EchoExplosionPrefab;
        
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            FusionGlitchNode node = nodes[i];
            if (node == null || (node != triggered && node.IsDead)) { nodes.RemoveAt(i); continue; }
            if (Vector2.Distance(node.transform.position, origin) > 5f) continue;
            
            Vector2 position = node.transform.position;
            nodes.RemoveAt(i);
            
            FusionCombatUtility.PlayExplosionFeedback(position, explosionVfx, 0.5f);
                
            FusionCombatUtility.DealArea(position, 2.25f, damage, Context);
            node.Consume();
        }
    }

    public override void Remove()
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
            if (nodes[i] != null) nodes[i].Consume();
        nodes.Clear();
        base.Remove();
    }
}

public class SupernovaModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_SUPERNOVA";
    private int hitCount;
    private readonly HashSet<IDamageable> cores = new HashSet<IDamageable>();

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        if (cores.Remove(hit.target))
        {
            Vector2 center = hit.target.Transform.position;
            FusionCombatUtility.PullArea(center, 4f, 2.5f, Context);
            FusionCombatUtility.DealArea(center, 3.5f, Mathf.Max(1, hit.finalDamage * 2), Context);
            FusionCombatUtility.ApplyBurnArea(center, 3.5f, 4f, Context);
            
            GameObject explosionVfx = Context.EchoExplosionPrefab;
            FusionCombatUtility.PlayExplosionFeedback(center, explosionVfx, 0.9f);
            return;
        }
        if (++hitCount % 3 == 0) cores.Add(hit.target);
    }

    public override void Remove() { base.Remove(); cores.Clear(); }
}

public class DeathDriveModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_DEATH_DRIVE";
    private readonly Dictionary<IDamageable, int> fracture = new Dictionary<IDamageable, int>();

    public override void Initialize(EchoModifierContext context)
    {
        base.Initialize(context);
        if (Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied += BeforeDamage;
    }

    public override void Remove()
    {
        if (Context != null && Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied -= BeforeDamage;
        fracture.Clear();
        base.Remove();
    }

    private void BeforeDamage(IDamageable target, ref DamageInfo damage)
    {
        if (damage.activeEcho == null || damage.activeEcho.uniqueModifierID != ModifierId || Context.PlayerHealth == null) return;
        damage.linearModifierSum += Mathf.Clamp01(1f - Context.PlayerHealth.HPPercent);
    }

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        if (Context.PlayerHealth == null || Context.PlayerHealth.HPPercent > 0.35f) return;
        EchoStatusReceiver status = FusionCombatUtility.GetStatus(hit.target);
        if (status == null) return;
        
        bool frozen = status.IsFrozen;
        bool slowed = status.IsSlowed;
        if (!frozen && !slowed) return;
        
        int stacks = frozen ? 3 : (fracture.TryGetValue(hit.target, out int current) ? current + 1 : 1);
        if (stacks < 3) { fracture[hit.target] = stacks; return; }
        
        fracture.Remove(hit.target);
        if (frozen) status.ForceRemoveFreeze();
        
        FusionCombatUtility.DealArea(hit.target.Transform.position, 3f,
            Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 1.6f)), Context, hit.target);
            
        GameObject explosionVfx = Context.EchoExplosionPrefab;
        FusionCombatUtility.PlayExplosionFeedback(hit.target.Transform.position, explosionVfx);
    }

}

public class CryoStasisModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_CRYO_STASIS";
    private readonly Dictionary<IDamageable, int> stacks = new Dictionary<IDamageable, int>();

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        EchoStatusReceiver status = FusionCombatUtility.GetStatus(hit.target);
        if (status.IsFrozen)
        {
            status.ForceRemoveFreeze();
            int burst = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 2.5f));
            FusionCombatUtility.DealDamage(hit.target, burst, Context, DamageSourceType.FusionSecondary, true);
            FusionCombatUtility.DealArea(hit.target.Transform.position, 2.75f,
                Mathf.Max(1, hit.finalDamage), Context, hit.target);
            stacks.Remove(hit.target);
            
            GameObject explosionVfx = Context.EchoExplosionPrefab;
            FusionCombatUtility.PlayExplosionFeedback(hit.target.Transform.position, explosionVfx);
            return;
        }

        int count = stacks.TryGetValue(hit.target, out int current) ? current + 1 : 1;
        if (count >= 3) { stacks.Remove(hit.target); status.ApplyFreeze(4f); }
        else stacks[hit.target] = count;
    }

    public override void Remove() { base.Remove(); stacks.Clear(); }
}

public class EventHorizonModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_EVENT_HORIZON";
    private int hitCount;
    private FusionRuntimeHost host;

    public override void Initialize(EchoModifierContext context)
    {
        base.Initialize(context);
        host = context.PlayerGameObject.GetComponent<FusionRuntimeHost>();
    }

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        if (++hitCount % 5 != 0 || host == null) return;
        int pulse = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.45f));
        int collapse = Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 2.2f));
        host.StartField(hit.target.Transform.position, 4f, 1f, 4.5f, pulse, collapse,
            Context, true, Context.EventHorizonPrefab, true, false, true);
    }

    public override void Remove()
    {
        if (host != null) host.StopAllFields();
        base.Remove();
    }
}

public class RagnarokModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_RAGNAROK";
    private int hitCount;

    public override void Initialize(EchoModifierContext context)
    {
        base.Initialize(context);
        if (Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied += BeforeDamage;
        if (Context.PlayerHealth != null)
        {
            Context.PlayerHealth.SetMaxHPCap(this, 100);
            Context.PlayerHealth.SetHealingBlocked(this, true);
            Context.PlayerHealth.SetIFramesDisabled(this, true);
            Context.PlayerHealth.OnBeforeTakeDamage += FatalDamage;
        }
    }

    public override void Remove()
    {
        if (Context != null && Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied -= BeforeDamage;
        if (Context != null && Context.PlayerHealth != null)
        {
            Context.PlayerHealth.OnBeforeTakeDamage -= FatalDamage;
            Context.PlayerHealth.SetHealingBlocked(this, false);
            Context.PlayerHealth.SetIFramesDisabled(this, false);
            Context.PlayerHealth.SetMaxHPCap(this, 0);
        }
        base.Remove();
    }

    private void BeforeDamage(IDamageable target, ref DamageInfo damage)
    {
        if (damage.activeEcho != null && damage.activeEcho.uniqueModifierID == ModifierId)
            damage.multiplicativeStack *= 3f;
    }

    private void FatalDamage(ref int amount, ref DamageInfo damage)
    {
        if (amount <= 0) return;
        amount = Mathf.Max(amount, Context.PlayerHealth.CurrentHP);
        damage.isTrueDamage = true;
    }

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        if (++hitCount % 5 != 0) return;
        FusionCombatUtility.DealArea(hit.target.Transform.position, 5f,
            Mathf.Max(1, hit.finalDamage * 3), Context, hit.target);
            
        GameObject explosionVfx = Context.EchoExplosionPrefab;
        FusionCombatUtility.PlayExplosionFeedback(hit.target.Transform.position, explosionVfx, 1.15f);
    }
}

public class ZeroPointModifier : FusionHitModifier
{
    protected override string ModifierId => "FUS_ZERO_POINT";
    private int hitCount;
    private FusionRuntimeHost host;

    public override void Initialize(EchoModifierContext context)
    {
        base.Initialize(context);
        host = context.PlayerGameObject.GetComponent<FusionRuntimeHost>();
        if (Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied += BeforeDamage;
    }

    private void BeforeDamage(IDamageable target, ref DamageInfo damage)
    {
        if (damage.activeEcho == null || damage.activeEcho.uniqueModifierID != ModifierId) return;
        EchoStatusReceiver status = FusionCombatUtility.GetStatus(target);
        if (status != null && status.IsFrozen) damage.isTrueDamage = true;
    }

    protected override void OnFusionHit(AttackHitbox.HitEventArgs hit)
    {
        EchoStatusReceiver status = FusionCombatUtility.GetStatus(hit.target);
        if (status != null && status.IsFrozen)
        {
            FusionCombatUtility.DealAreaToFrozen(hit.target.Transform.position, 5f,
                Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.6f)), Context, hit.target);
        }
        if (++hitCount % 4 != 0 || host == null) return;
        host.StartField(hit.target.Transform.position, 4f, 1f, 4.5f,
            Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 0.35f)),
            Mathf.Max(1, Mathf.RoundToInt(hit.finalDamage * 1.5f)),
            Context, true, Context.GlitchedZonePrefab, false, true, false, true);
    }

    public override void Remove()
    {
        if (Context != null && Context.PlayerAttackHitbox != null) Context.PlayerAttackHitbox.OnBeforeDamageApplied -= BeforeDamage;
        if (host != null) host.StopAllFields();
        base.Remove();
    }
}
