using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lifecycle host for one generated Relic strategy. Concrete strategies live in
/// separate catalog files and subscribe only to events they use.
/// </summary>
public sealed class UniversalRelicEffect : MonoBehaviour, IRelicEffect
{
    private RelicRuntimeBehavior behavior;

    public static bool Supports(string itemId) => RelicRuntimeBehaviorFactory.Supports(itemId);

    public void OnEquip(PlayerEventBus eventBus, PlayerRelicManager relicManager, string itemID)
    {
        RelicRuntimeContext context = new RelicRuntimeContext(this, eventBus, relicManager);
        behavior = RelicRuntimeBehaviorFactory.Create(itemID, context);
        if (behavior == null)
        {
            Debug.LogError($"[RelicRuntime] No strategy registered for '{itemID}'.", this);
            return;
        }
        behavior.Equip();
    }

    public void OnUnequip(PlayerEventBus eventBus)
    {
        behavior?.Unequip();
        behavior = null;
        StopAllCoroutines();
    }

    private void Update() => behavior?.Tick();

    public Coroutine StartBehaviorCoroutine(IEnumerator routine) => routine != null ? StartCoroutine(routine) : null;
    public void StopBehaviorCoroutine(Coroutine routine) { if (routine != null) StopCoroutine(routine); }
}

internal sealed class RelicRuntimeContext
{
    public RelicRuntimeContext(UniversalRelicEffect host, PlayerEventBus events, PlayerRelicManager manager)
    {
        Host = host;
        Events = events;
        Manager = manager;
        Player = host.gameObject;
        Health = host.GetComponent<HealthSystem>();
        Movement = host.GetComponent<PlayerMovement>();
        Attack = host.GetComponent<PlayerAttack>();
        Modifiers = host.GetComponent<PlayerRuntimeModifiers>();
        if (Modifiers == null) Modifiers = host.gameObject.AddComponent<PlayerRuntimeModifiers>();
    }

    public UniversalRelicEffect Host { get; }
    public PlayerEventBus Events { get; }
    public PlayerRelicManager Manager { get; }
    public GameObject Player { get; }
    public HealthSystem Health { get; }
    public PlayerMovement Movement { get; }
    public PlayerAttack Attack { get; }
    public PlayerRuntimeModifiers Modifiers { get; }
    public object Source => Host;

    public void ForceCritical(ref DamageInfo info)
    {
        if (info.isCritical) return;
        info.isCritical = true;
        info.multiplicativeStack *= Health != null && Health.CombatStats != null
            ? Health.CombatStats.critMultiplier
            : 1.5f;
    }

    public void DealAreaDamage(Vector2 center, float radius, int damage, IDamageable excluded = null, int maxTargets = 32)
    {
        int struck = 0;
        foreach (IDamageable target in FindTargets(center, radius))
        {
            if (ReferenceEquals(target, excluded)) continue;
            DamageInfo area = DamageInfo.Create(Mathf.Max(1, damage), Player);
            area.damageSource = DamageSourceType.RelicArea;
            target.TakeDamage(area);
            if (++struck >= maxTargets) break;
        }
    }

    public void ChainDamage(IDamageable origin, int damage, int maxTargets)
    {
        int struck = 0;
        foreach (IDamageable target in FindTargets(origin.Transform.position, 7f))
        {
            if (ReferenceEquals(target, origin)) continue;
            DamageInfo chain = DamageInfo.Create(Mathf.Max(1, damage), Player);
            chain.damageSource = DamageSourceType.RelicSecondary;
            target.TakeDamage(chain);
            if (++struck >= maxTargets) break;
        }
    }

    public static List<IDamageable> FindTargets(Vector2 center, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, EnemyMask);
        List<IDamageable> targets = new List<IDamageable>();
        HashSet<IDamageable> unique = new HashSet<IDamageable>();
        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (!IsValid(target) || target.IsDead || !unique.Add(target)) continue;
            targets.Add(target);
        }
        targets.Sort((a, b) => Vector2.SqrMagnitude((Vector2)a.Transform.position - center)
            .CompareTo(Vector2.SqrMagnitude((Vector2)b.Transform.position - center)));
        return targets;
    }

    public static bool IsValid(IDamageable target)
    {
        if (target == null) return false;
        if (target is UnityEngine.Object unityObject && unityObject == null) return false;
        return target.Transform != null;
    }

    public static int EnemyMask
    {
        get
        {
            int mask = LayerMask.GetMask("Enemy");
            return mask != 0 ? mask : ~0;
        }
    }
}

internal sealed class RelicRuntimeBehavior
{
    private readonly RelicRuntimeContext context;

    public RelicRuntimeBehavior(RelicRuntimeContext context) => this.context = context;

    public Action OnEquip;
    public Action OnUnequip;
    public Action OnTick;
    public PlayerEventBus.DamageModifierHandler IncomingDamage;
    public int IncomingDamagePriority = 100;
    public PlayerEventBus.FatalDamageHandler FatalDamage;
    public PlayerEventBus.OutgoingDamageHandler OutgoingDamage;
    public Action<AttackHitbox.HitEventArgs> SuccessfulHit;
    public Action<PlayerEventBus.EnemyKillEvent> EnemyKilled;
    public Action<Room> RoomEntered;
    public Action PickupCollected;
    public Action<PlayerAttack.PlungeImpactEventArgs> PlungeImpact;
    public EventHandler<PlayerAttack.AttackEventArgs> AttackStarted;
    public EventHandler Dash;
    public EventHandler WallJump;
    public Action<int> JumpPerformed;

    public void Equip()
    {
        PlayerEventBus bus = context.Events;
        if (IncomingDamage != null) bus.RegisterDamageModifier(IncomingDamage, IncomingDamagePriority);
        if (FatalDamage != null) bus.OnFatalDamage += FatalDamage;
        if (OutgoingDamage != null) bus.OnBeforeOutgoingDamage += OutgoingDamage;
        if (SuccessfulHit != null) bus.OnSuccessfulHit += SuccessfulHit;
        if (EnemyKilled != null) bus.OnEnemyKilledDetailed += EnemyKilled;
        if (RoomEntered != null) bus.OnRoomEntered += RoomEntered;
        if (PickupCollected != null) bus.OnPickupCollected += PickupCollected;
        if (PlungeImpact != null) bus.OnPlungeImpact += PlungeImpact;
        if (AttackStarted != null && context.Attack != null) context.Attack.OnAttackStarted += AttackStarted;
        if (context.Movement != null)
        {
            if (Dash != null) context.Movement.OnDash += Dash;
            if (WallJump != null) context.Movement.OnWallJump += WallJump;
            if (JumpPerformed != null) context.Movement.OnJumpPerformed += JumpPerformed;
        }
        OnEquip?.Invoke();
    }

    public void Unequip()
    {
        PlayerEventBus bus = context.Events;
        if (IncomingDamage != null) bus.UnregisterDamageModifier(IncomingDamage);
        if (FatalDamage != null) bus.OnFatalDamage -= FatalDamage;
        if (OutgoingDamage != null) bus.OnBeforeOutgoingDamage -= OutgoingDamage;
        if (SuccessfulHit != null) bus.OnSuccessfulHit -= SuccessfulHit;
        if (EnemyKilled != null) bus.OnEnemyKilledDetailed -= EnemyKilled;
        if (RoomEntered != null) bus.OnRoomEntered -= RoomEntered;
        if (PickupCollected != null) bus.OnPickupCollected -= PickupCollected;
        if (PlungeImpact != null) bus.OnPlungeImpact -= PlungeImpact;
        if (AttackStarted != null && context.Attack != null) context.Attack.OnAttackStarted -= AttackStarted;
        if (context.Movement != null)
        {
            if (Dash != null) context.Movement.OnDash -= Dash;
            if (WallJump != null) context.Movement.OnWallJump -= WallJump;
            if (JumpPerformed != null) context.Movement.OnJumpPerformed -= JumpPerformed;
        }
        OnUnequip?.Invoke();
    }

    public void Tick() => OnTick?.Invoke();
}

internal interface IRelicRuntimeStrategy
{
    RelicRuntimeBehavior Create(RelicRuntimeContext context);
}

internal static class RelicRuntimeBehaviorFactory
{
    private static readonly Dictionary<string, IRelicRuntimeStrategy> Strategies =
        new Dictionary<string, IRelicRuntimeStrategy>(StringComparer.Ordinal)
        {
            ["GraftingFlask"] = new GraftingFlaskStrategy(),
            ["BloodthirstyMoss"] = new BloodthirstyMossStrategy(),
            ["AcidicGallbladder"] = new AcidicGallbladderStrategy(),
            ["DarkBandage"] = new DarkBandageStrategy(),
            ["ShatteredMemory"] = new ShatteredMemoryStrategy(),
            ["RustyGrapple"] = new RustyGrappleStrategy(),
            ["RottenWeb"] = new RottenWebStrategy(),
            ["BurrowersScale"] = new BurrowersScaleStrategy(),
            ["ToxicSpore"] = new ToxicSporeStrategy(),
            ["RustedCoin"] = new RustedCoinStrategy(),
            ["EmberFirefly"] = new EmberFireflyStrategy(),
            ["SpikedCleats"] = new SpikedCleatsStrategy(),
            ["EchoWhetstone"] = new EchoWhetstoneStrategy(),
            ["BouncerShroom"] = new BouncerShroomStrategy(),
            ["VialOfAshes"] = new VialOfAshesStrategy(),
            ["BatsTalon"] = new BatsTalonStrategy(),
            ["RustyHeavyChain"] = new RustyHeavyChainStrategy(),
            ["DriedCyclopsEye"] = new DriedCyclopsEyeStrategy(),
            ["VolatileCore"] = new VolatileCoreStrategy(),
            ["BloodContract"] = new PassiveRelicStrategy(),
            ["OreSparkCore"] = new OreSparkCoreStrategy(),
            ["StalactiteHeart"] = new StalactiteHeartStrategy(),
            ["SoulBell"] = new SoulBellStrategy(),
            ["CondemnedRing"] = new CondemnedRingStrategy(),
            ["EchoingSigil"] = new EchoingSigilStrategy(),
            ["AbyssalTreads"] = new AbyssalTreadsStrategy(),
            ["VampiricFang"] = new VampiricFangStrategy()
        };

    public static bool Supports(string itemId) => !string.IsNullOrEmpty(itemId) && Strategies.ContainsKey(itemId);

    public static RelicRuntimeBehavior Create(string itemId, RelicRuntimeContext context) =>
        Strategies.TryGetValue(itemId, out IRelicRuntimeStrategy strategy) ? strategy.Create(context) : null;
}
