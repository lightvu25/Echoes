#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class MindWorldModifierTests
{
    private static readonly PropertyInfo GameSessionInstanceProperty =
        typeof(GameSession).GetProperty(nameof(GameSession.Instance), BindingFlags.Public | BindingFlags.Static);

    private GameSession previousSession;
    private GameObject sessionObject;
    private GameObject managerObject;
    private GameSession session;
    private MindPathManager manager;

    [SetUp]
    public void SetUp()
    {
        previousSession = GameSession.Instance;

        sessionObject = new GameObject("Mind Modifier Test Session");
        sessionObject.SetActive(false);
        session = sessionObject.AddComponent<GameSession>();
        session.currentRun = new RunData();
        SetGameSessionInstance(session);

        managerObject = new GameObject("Mind Modifier Test Manager");
        managerObject.SetActive(false);
        manager = managerObject.AddComponent<MindPathManager>();
    }

    [TearDown]
    public void TearDown()
    {
        SetGameSessionInstance(previousSession);
        if (managerObject != null) Object.DestroyImmediate(managerObject);
        if (sessionObject != null) Object.DestroyImmediate(sessionObject);
    }

    [Test]
    public void ModifierNodePrefabs_AreLinkedToTheExpectedData()
    {
        MindNode relic = LoadNode("Assets/Prefabs/Mind World/RelicNode.prefab", NodeType.Relic);
        MindNode echo = LoadNode("Assets/Prefabs/Mind World/EchoNode.prefab", NodeType.Echo);
        MindNode equipment = LoadNode("Assets/Prefabs/Mind World/EquipmentNode.prefab", NodeType.Equipment);

        Assert.AreEqual(0.5f, relic.ModifierData.bonusRelicChance);
        Assert.AreEqual(1.5f, relic.ModifierData.enemyDensityMultiplier);
        CollectionAssert.Contains(relic.ModifierData.addedEliteEnemyTypes, "Elite");

        Assert.AreEqual(0.5f, echo.ModifierData.bonusEchoChance);
        Assert.AreEqual(10, echo.ModifierData.magicToxicityIncrease);
        Assert.AreEqual(1.2f, echo.ModifierData.enemyDensityMultiplier);

        Assert.AreEqual(0.5f, equipment.ModifierData.bonusEquipmentChance);
        Assert.AreEqual(2f, equipment.ModifierData.enemyDensityMultiplier);
    }

    [Test]
    public void AcceptingAllModifierNodes_AppliesRewardsAndRisksToTheRun()
    {
        RunData run = session.currentRun;

        manager.AcceptNodePath(LoadNode("Assets/Prefabs/Mind World/RelicNode.prefab", NodeType.Relic));
        Assert.That(run.minGuaranteedRelics, Is.InRange(1, 2));
        Assert.AreEqual(0.5f, run.bonusRelicChance);
        Assert.AreEqual(1.5f, run.enemyDensityMultiplier);
        CollectionAssert.AreEqual(new[] { "Elite" }, run.addedEliteEnemyTypes);

        manager.AcceptNodePath(LoadNode("Assets/Prefabs/Mind World/EchoNode.prefab", NodeType.Echo));
        Assert.That(run.minGuaranteedEchoes, Is.InRange(1, 2));
        Assert.AreEqual(0.5f, run.bonusEchoChance);
        Assert.AreEqual(10, run.magicToxicity);
        Assert.AreEqual(1.8f, run.enemyDensityMultiplier, 0.0001f);

        manager.AcceptNodePath(LoadNode("Assets/Prefabs/Mind World/EquipmentNode.prefab", NodeType.Equipment));
        Assert.That(run.minGuaranteedEquipment, Is.InRange(1, 2));
        Assert.AreEqual(0.5f, run.bonusEquipmentChance);
        Assert.AreEqual(3.6f, run.enemyDensityMultiplier, 0.0001f);
    }

    [Test]
    public void LootResolver_ExposesMindWorldBonusesToLootRolls()
    {
        session.currentRun.bonusRelicChance = 0.25f;
        session.currentRun.bonusEchoChance = 0.35f;
        session.currentRun.bonusEquipmentChance = 0.45f;
        session.currentRun.currentLevelRelicMultiplier = 1.1f;
        session.currentRun.currentLevelEchoMultiplier = 1.2f;
        session.currentRun.currentLevelEquipmentMultiplier = 1.3f;

        LootBonuses bonuses = LootBonusResolver.Resolve();

        Assert.AreEqual(0.25f, bonuses.relicBonus);
        Assert.AreEqual(0.35f, bonuses.echoBonus);
        Assert.AreEqual(0.45f, bonuses.equipmentBonus);
        Assert.AreEqual(1.1f, bonuses.roomRelicMultiplier);
        Assert.AreEqual(1.2f, bonuses.roomEchoMultiplier);
        Assert.AreEqual(1.3f, bonuses.roomEquipmentMultiplier);
    }

    [Test]
    public void GuaranteedMindWorldDrops_AreConsumedByTheirMatchingLootTypes()
    {
        session.currentRun.minGuaranteedRelics = 1;
        session.currentRun.minGuaranteedEchoes = 1;
        session.currentRun.minGuaranteedEquipment = 1;

        GameObject relicPrefab = new GameObject("Guaranteed Relic");
        GameObject echoPrefab = new GameObject("Guaranteed Echo");
        GameObject equipmentPrefab = new GameObject("Guaranteed Equipment");
        LootTable table = ScriptableObject.CreateInstance<LootTable>();

        try
        {
            table.lootItems = new List<LootItem>
            {
                NewLootItem(relicPrefab, LootItemType.Relic),
                NewLootItem(echoPrefab, LootItemType.Echo),
                NewLootItem(equipmentPrefab, LootItemType.Equipment)
            };

            List<DropResult> drops = table.GetDrops(1f, 1);

            Assert.AreEqual(3, drops.Count);
            Assert.AreEqual(0, session.currentRun.minGuaranteedRelics);
            Assert.AreEqual(0, session.currentRun.minGuaranteedEchoes);
            Assert.AreEqual(0, session.currentRun.minGuaranteedEquipment);
        }
        finally
        {
            Object.DestroyImmediate(table);
            Object.DestroyImmediate(relicPrefab);
            Object.DestroyImmediate(echoPrefab);
            Object.DestroyImmediate(equipmentPrefab);
        }
    }

    [Test]
    public void EnemyDensityModifier_ReachesRuntimeEnemyPopulation()
    {
        session.currentRun.enemyDensityMultiplier = 2f;
        GameObject baseEnemy = new GameObject("Base Enemy");
        LevelBlueprint blueprint = CreateEnemyBlueprint(baseEnemy);
        GameObject roomObject = CreateRoomWithGroundAnchor(out Room room);

        try
        {
            RuntimeLevelPopulator populator = managerObject.AddComponent<RuntimeLevelPopulator>();
            ExpectEditModeDestroyError();
            populator.PopulateRooms(new List<Room> { room }, blueprint);

            Transform spawned = roomObject.transform.Find("EnemiesContainer/Base Enemy");
            Assert.NotNull(spawned, "A 0.5 base spawn chance multiplied by 2 density should guarantee the enemy spawn.");
        }
        finally
        {
            Object.DestroyImmediate(roomObject);
            Object.DestroyImmediate(blueprint);
            Object.DestroyImmediate(baseEnemy);
        }
    }

    [Test]
    public void ToxicityModifier_SpawnsConfiguredEliteAtOneHundredPercent()
    {
        session.currentRun.magicToxicity = 100;
        session.currentRun.addedEliteEnemyTypes.Add("Elite");

        GameObject baseEnemy = new GameObject("Base Enemy");
        GameObject eliteEnemy = new GameObject("Elite");
        LevelBlueprint blueprint = CreateEnemyBlueprint(baseEnemy);
        blueprint.groundEnemySpawnChance = 1f;
        blueprint.availableElitePrefabs.Add(eliteEnemy);
        GameObject roomObject = CreateRoomWithGroundAnchor(out Room room);

        try
        {
            RuntimeLevelPopulator populator = managerObject.AddComponent<RuntimeLevelPopulator>();
            ExpectEditModeDestroyError();
            populator.PopulateRooms(new List<Room> { room }, blueprint);

            Transform spawned = roomObject.transform.Find("EnemiesContainer/Elite");
            Assert.NotNull(spawned, "100 toxicity should replace the base enemy with the registered Elite prefab.");
        }
        finally
        {
            Object.DestroyImmediate(roomObject);
            Object.DestroyImmediate(blueprint);
            Object.DestroyImmediate(baseEnemy);
            Object.DestroyImmediate(eliteEnemy);
        }
    }

    [Test]
    public void RelicEliteRisk_HasAMatchingPrefabInTheActiveBlueprint()
    {
        LevelBlueprint blueprint = AssetDatabase.LoadAssetAtPath<LevelBlueprint>("Assets/Data/Blueprints/The Abyss Blueprint.asset");
        MindNode relic = LoadNode("Assets/Prefabs/Mind World/RelicNode.prefab", NodeType.Relic);

        Assert.NotNull(blueprint);
        foreach (string eliteName in relic.ModifierData.addedEliteEnemyTypes)
        {
            Assert.IsTrue(
                blueprint.availableElitePrefabs.Exists(prefab => prefab != null && prefab.name == eliteName),
                $"Relic modifier requests elite '{eliteName}', but The Abyss Blueprint has no matching prefab registered.");
        }
    }

    private static MindNode LoadNode(string path, NodeType expectedType)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.NotNull(prefab, $"Missing node prefab at {path}");

        MindNode node = prefab.GetComponent<MindNode>();
        Assert.NotNull(node, $"{path} has no MindNode component");
        Assert.AreEqual(expectedType, node.nodeType, $"{path} has the wrong node type");
        Assert.NotNull(node.ModifierData, $"{path} has no modifier data assigned");
        return node;
    }

    private static LootItem NewLootItem(GameObject prefab, LootItemType type)
    {
        return new LootItem
        {
            itemPrefab = prefab,
            type = type,
            dropChance = 0f,
            itemTier = 1,
            minAmount = 1,
            maxAmount = 1
        };
    }

    private static LevelBlueprint CreateEnemyBlueprint(GameObject baseEnemy)
    {
        LevelBlueprint blueprint = ScriptableObject.CreateInstance<LevelBlueprint>();
        blueprint.groundEnemySpawnChance = 0.5f;
        blueprint.groundEnemyPool.Add(new EnemyNodeRate { enemyPrefab = baseEnemy, weight = 1f });
        return blueprint;
    }

    private static GameObject CreateRoomWithGroundAnchor(out Room room)
    {
        GameObject roomObject = new GameObject("Mind Modifier Test Room");
        room = roomObject.AddComponent<Room>();

        GameObject anchorObject = new GameObject("Ground Enemy Anchor");
        anchorObject.transform.SetParent(roomObject.transform);
        EntityAnchor anchor = anchorObject.AddComponent<EntityAnchor>();
        anchor.anchorType = AnchorType.Enemy_Ground;
        return roomObject;
    }

    private static void ExpectEditModeDestroyError()
    {
        LogAssert.Expect(
            LogType.Error,
            "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
    }

    private static void SetGameSessionInstance(GameSession value)
    {
        MethodInfo setter = GameSessionInstanceProperty?.GetSetMethod(true);
        if (setter == null) throw new InvalidOperationException("Could not access GameSession.Instance setter for tests.");
        setter.Invoke(null, new object[] { value });
    }
}
#endif
