#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class RelicEquipmentCatalogTests
{
    private static readonly HashSet<string> RelicIds = new HashSet<string>
    {
        "GraftingFlask", "BloodthirstyMoss", "AcidicGallbladder", "DarkBandage", "ShatteredMemory",
        "RustyGrapple", "RottenWeb", "BurrowersScale", "ToxicSpore", "RustedCoin", "EmberFirefly",
        "SpikedCleats", "EchoWhetstone", "BouncerShroom", "VialOfAshes", "BatsTalon",
        "RustyHeavyChain", "DriedCyclopsEye", "VolatileCore", "BloodContract", "OreSparkCore",
        "StalactiteHeart", "SoulBell", "CondemnedRing", "EchoingSigil", "AbyssalTreads", "VampiricFang"
    };

    private static readonly HashSet<string> ToolIds = new HashSet<string>
    {
        "BOMB", "CRIMSON_DART", "ADRENALINE_VIAL", "ARACHNE_TRAP", "TOXIC_FLASK", "VOID_AEGIS", "KINETIC_ROOT"
    };

    [Test]
    public void RelicCatalog_AllGeneratedAssetsHaveRuntimeBehavior()
    {
        HashSet<string> found = LoadIds<RelicData>("Assets/Data/Relics");
        CollectionAssert.AreEquivalent(RelicIds, found);
        foreach (string id in found) Assert.IsTrue(PlayerRelicManager.SupportsRelic(id), $"Missing Relic behavior for {id}");
    }

    [Test]
    public void EquipmentCatalog_AllGeneratedAssetsHaveRuntimeBehavior()
    {
        HashSet<string> found = LoadIds<ToolData>("Assets/Data/Equipments");
        CollectionAssert.AreEquivalent(ToolIds, found);
        foreach (string id in found) Assert.IsTrue(EquipmentRuntimeRegistry.SupportsTool(id), $"Missing Equipment behavior for {id}");
    }

    [Test]
    public void PlayerPrefab_ContainsCompleteEquipmentRegistry()
    {
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Entities/Player/Player.prefab");
        Assert.NotNull(player);
        Assert.NotNull(player.GetComponent<PlayerTool>());
        Assert.NotNull(player.GetComponent<PlayerEventBus>());
        Assert.NotNull(player.GetComponent<PlayerRelicManager>());

        EquipmentRuntimeRegistry registry = player.GetComponent<EquipmentRuntimeRegistry>();
        Assert.NotNull(registry);
        foreach (string id in ToolIds) Assert.IsTrue(registry.HasExecutor(id), $"Missing concrete executor for {id}");
        SerializedObject serializedRegistry = new SerializedObject(registry);
        foreach (string field in new[]
        {
            "bloodOreBombPrefab", "crimsonDartPrefab", "arachneTrapPrefab",
            "toxicFlaskPrefab", "toxicCloudPrefab", "kineticRootPrefab"
        })
        {
            Assert.NotNull(serializedRegistry.FindProperty(field)?.objectReferenceValue, $"Registry field {field} is missing");
        }
    }

    [UnityTest]
    public IEnumerator MaxHpRelics_ComposeAcrossEquipOrderAndPermanentGains()
    {
        yield return new EnterPlayMode();

        GameObject player = new GameObject("Max HP Relic Test");
        try
        {
            HealthSystem health = player.AddComponent<HealthSystem>();
            int baseMaxHp = health.MaxHP;
            object flask = new object();
            object ring = new object();

            health.SetMaxHPCap(ring, 1);
            health.SetMaxHPModifier(flask, 100, true);
            Assert.AreEqual(1, health.MaxHP, "Condemned Ring must win regardless of equip order.");

            health.ModifyMaxHP(25);
            Assert.AreEqual(1, health.MaxHP, "Permanent gains must remain hidden while the cap is active.");

            health.SetMaxHPCap(ring, 0);
            Assert.AreEqual(baseMaxHp + 125, health.MaxHP);

            health.SetMaxHPModifier(flask, 0);
            Assert.AreEqual(baseMaxHp + 25, health.MaxHP, "Removing Grafting Flask must preserve permanent gains.");

            health.SetMaxHPCap(ring, 1);
            health.SetMaxHPModifier(flask, 100);
            health.SetMaxHPModifier(flask, 0);
            health.SetMaxHPCap(ring, 0);
            Assert.AreEqual(baseMaxHp + 25, health.MaxHP, "Reverse removal order must preserve base HP.");

            health.SetMaxHPModifier(flask, 100);
            health.SetMaxHP(300);
            Assert.AreEqual(300, health.MaxHP, "SetMaxHP remains an absolute effective value with additives active.");
            health.SetMaxHPModifier(flask, 0);
            Assert.AreEqual(200, health.MaxHP);

            int progressionEvents = 0;
            health.OnMaxHPGained += () => progressionEvents++;
            health.SetMaxHPModifier(flask, 100);
            Assert.AreEqual(0, progressionEvents, "Temporary Relic modifiers must not grant progression unlocks.");
            health.ModifyMaxHP(1);
            Assert.AreEqual(1, progressionEvents, "Permanent gains should still notify progression.");
        }
        finally
        {
            Object.Destroy(player);
        }

        yield return null;
        yield return new ExitPlayMode();
    }

    [Test]
    public void ElitePrefab_UsesExplicitEliteRankForSoulBell()
    {
        GameObject elite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Entities/Enemy/Elite Enemy.prefab");
        Assert.NotNull(elite);
        EnemyCombat combat = elite.GetComponent<EnemyCombat>();
        Assert.NotNull(combat);
        Assert.AreEqual(EnemyRank.Elite, combat.Rank);
        Assert.IsTrue(combat.IsEliteOrBoss);
    }

    [Test]
    public void Pool_SameNameObjectsRemainInSeparateSourcePools()
    {
        ObjectPoolManager.ObjectPools.Clear();
        GameObject sourceA = new GameObject("Bomb");
        GameObject sourceB = new GameObject("Bomb");
        GameObject firstA = null;
        GameObject firstB = null;
        try
        {
            firstA = ObjectPoolManager.SpawnObject(sourceA, Vector3.zero, Quaternion.identity);
            firstB = ObjectPoolManager.SpawnObject(sourceB, Vector3.one, Quaternion.identity);
            ObjectPoolManager.ReturnObjectToPool(firstA);
            ObjectPoolManager.ReturnObjectToPool(firstB);

            GameObject secondA = ObjectPoolManager.SpawnObject(sourceA, Vector3.zero, Quaternion.identity);
            GameObject secondB = ObjectPoolManager.SpawnObject(sourceB, Vector3.one, Quaternion.identity);
            Assert.AreSame(firstA, secondA);
            Assert.AreSame(firstB, secondB);
            Assert.AreNotSame(secondA, secondB);
        }
        finally
        {
            foreach (PooledObjectInfo pool in ObjectPoolManager.ObjectPools)
                foreach (GameObject pooled in pool.pooledObjects)
                    if (pooled != null) Object.DestroyImmediate(pooled);
            ObjectPoolManager.ObjectPools.Clear();
            Object.DestroyImmediate(sourceA);
            Object.DestroyImmediate(sourceB);
        }
    }

    [Test]
    public void DamageOverTimeAndRelicSecondary_BypassOrdinaryIFrames()
    {
        Assert.IsTrue(new DamageInfo { damageSource = DamageSourceType.Poison }.BypassesInvincibilityFrames);
        Assert.IsTrue(new DamageInfo { damageSource = DamageSourceType.Burn }.BypassesInvincibilityFrames);
        Assert.IsTrue(new DamageInfo { damageSource = DamageSourceType.RelicSecondary }.BypassesInvincibilityFrames);
        Assert.IsTrue(new DamageInfo { damageSource = DamageSourceType.RelicArea }.BypassesInvincibilityFrames);
        Assert.IsFalse(new DamageInfo { damageSource = DamageSourceType.Attack }.BypassesInvincibilityFrames);
    }

    [Test]
    public void BurnOwnership_ReapplicationDoesNotInheritAnOldAttacker()
    {
        GameObject target = new GameObject("Burn Target");
        GameObject player = new GameObject("Burn Owner");
        try
        {
            EchoStatusReceiver status = target.AddComponent<EchoStatusReceiver>();
            status.ApplyBurn(1f, null, player);
            Assert.AreSame(player, status.BurnAttacker);

            status.ApplyBurn(1f);
            Assert.IsNull(status.BurnAttacker, "A source-less burn must not inherit an earlier player's ownership.");

            status.ForceRemoveBurn();
            Assert.IsNull(status.BurnAttacker);
        }
        finally
        {
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(player);
        }
    }

    [UnityTest]
    public IEnumerator IncomingRelicModifiers_RunInDeterministicPriorityOrder()
    {
        yield return new EnterPlayMode();

        // A scene opened by the developer may already contain the real player.
        // Remove its play-mode-only bus so this test owns the singleton deterministically.
        if (PlayerEventBus.Instance != null)
        {
            Object.Destroy(PlayerEventBus.Instance);
            yield return null;
        }

        GameObject player = new GameObject("Incoming Priority Test");
        try
        {
            HealthSystem health = player.AddComponent<HealthSystem>();
            PlayerEventBus bus = player.AddComponent<PlayerEventBus>();
            List<int> order = new List<int>();
            int shieldObservedDamage = -1;

            void Shield(ref int damage, ref DamageInfo info) { order.Add(200); shieldObservedDamage = damage; }
            void Immunity(ref int damage, ref DamageInfo info) { order.Add(0); damage = 0; }
            void Reaction(ref int damage, ref DamageInfo info) { order.Add(100); }

            bus.RegisterDamageModifier(Shield, 200);
            bus.RegisterDamageModifier(Reaction, 100);
            bus.RegisterDamageModifier(Immunity, 0);
            health.TakeDamage(DamageInfo.Create(10, player));

            CollectionAssert.AreEqual(new[] { 0, 100, 200 }, order);
            Assert.AreEqual(0, shieldObservedDamage, "Later modifiers must observe changes made by earlier priorities.");
        }
        finally
        {
            Object.Destroy(player);
        }

        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTearDown]
    public IEnumerator RestoreEditModeAfterLifecycleTest()
    {
        if (EditorApplication.isPlaying)
            yield return new ExitPlayMode();
    }

    [Test]
    public void SuccessfulHitBridge_PreservesOriginatingAttackMetadata()
    {
        GameObject player = new GameObject("Successful Hit Bridge Test");
        GameObject targetObject = new GameObject("Target");
        try
        {
            player.AddComponent<HealthSystem>();
            PlayerEventBus bus = player.AddComponent<PlayerEventBus>();
            StubDamageable target = new StubDamageable(targetObject.transform);
            int calls = 0;
            AttackHitbox.HitEventArgs received = null;
            bus.OnSuccessfulHit += hit => { calls++; received = hit; };

            DamageInfo info = DamageInfo.Create(10, player);
            info.attackSequenceId = 17;
            info.hasPlayerAttackMetadata = true;
            info.originatingComboStep = 2;
            bus.FireSuccessfulHit(target, info, 13);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(17, received.damageInfo.attackSequenceId);
            Assert.AreEqual(2, received.damageInfo.originatingComboStep);
            Assert.AreEqual(13, received.finalDamage);
        }
        finally
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(targetObject);
        }
    }

    private sealed class StubDamageable : IDamageable
    {
        public StubDamageable(Transform transform) => Transform = transform;
        public bool IsDead => false;
        public Transform Transform { get; }
        public float Defense => 0f;
        public void TakeDamage(DamageInfo damageInfo) { }
    }

    private static HashSet<string> LoadIds<T>(string folder) where T : ItemBaseData
    {
        HashSet<string> ids = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.NotNull(asset);
            Assert.IsFalse(string.IsNullOrWhiteSpace(asset.itemID));
            Assert.IsTrue(ids.Add(asset.itemID), $"Duplicate ID {asset.itemID}");
        }
        return ids;
    }
}
#endif
