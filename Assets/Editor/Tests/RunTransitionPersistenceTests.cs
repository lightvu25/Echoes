#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RunTransitionPersistenceTests
{
    [Test]
    public void RunData_JsonRoundTripPreservesLoadoutHealthAndActiveEcho()
    {
        var run = new RunData
        {
            currentHealth = 137,
            maxHealth = 180,
            activeEchoIndex = 2,
            currentAstralShards = 47,
            equippedEchoIds = new List<string> { "ECHO_BLAZE", "", "ECHO_ARC" },
            equippedRelicIds = new List<string> { "StalactiteHeart" },
            equippedToolIds = new List<string> { "ADRENALINE_VIAL" }
        };

        RunData restored = JsonUtility.FromJson<RunData>(JsonUtility.ToJson(run));

        Assert.AreEqual(137, restored.currentHealth);
        Assert.AreEqual(180, restored.maxHealth);
        Assert.AreEqual(2, restored.activeEchoIndex);
        Assert.AreEqual(47, restored.currentAstralShards);
        CollectionAssert.AreEqual(run.equippedEchoIds, restored.equippedEchoIds);
        CollectionAssert.AreEqual(run.equippedRelicIds, restored.equippedRelicIds);
        CollectionAssert.AreEqual(run.equippedToolIds, restored.equippedToolIds);
    }

    [Test]
    public void RunItemCatalog_HasUniqueStableIdsForEveryRuntimeCategory()
    {
        RunItemCatalog catalog = AssetDatabase.LoadAssetAtPath<RunItemCatalog>(
            "Assets/Data/Player/Run Item Catalog.asset");
        Assert.NotNull(catalog);

        var ids = new HashSet<string>();
        bool foundEcho = false;
        bool foundRelic = false;
        bool foundTool = false;

        foreach (ItemBaseData item in catalog.Items)
        {
            Assert.NotNull(item);
            Assert.IsFalse(string.IsNullOrWhiteSpace(item.itemID));
            Assert.IsTrue(ids.Add(item.itemID), $"Duplicate run item ID: {item.itemID}");
            foundEcho |= item.Category == ItemCategory.Echo;
            foundRelic |= item.Category == ItemCategory.Relic;
            foundTool |= item.Category == ItemCategory.Tool;
        }

        Assert.IsTrue(foundEcho);
        Assert.IsTrue(foundRelic);
        Assert.IsTrue(foundTool);
    }

    [Test]
    public void PlayerPrefab_ReferencesRunItemCatalog()
    {
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/Entities/Player/Player.prefab");
        Assert.NotNull(player);

        PlayerInventoryCore inventory = player.GetComponent<PlayerInventoryCore>();
        Assert.NotNull(inventory);
        SerializedProperty catalogProperty = new SerializedObject(inventory).FindProperty("itemCatalog");
        Assert.NotNull(catalogProperty);
        Assert.NotNull(catalogProperty.objectReferenceValue);
    }

    [Test]
    public void MobLoot_ResourceDropsHavePositiveAmountsAndCompatibleCategories()
    {
        LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>("Assets/Data/Loots/MobLoot.asset");
        Assert.NotNull(table);

        bool foundAstralShard = false;
        foreach (LootItem item in table.lootItems)
        {
            Assert.NotNull(item.itemPrefab);
            if (!item.itemPrefab.TryGetComponent(out ResourceDrop resourceDrop)) continue;

            Assert.Greater(item.minAmount, 0);
            Assert.GreaterOrEqual(item.maxAmount, item.minAmount);
            Assert.IsTrue(item.type == LootItemType.Currency || item.type == LootItemType.Consumable,
                $"Resource prefab '{item.itemPrefab.name}' has incompatible category {item.type}.");

            SerializedProperty typeProperty = new SerializedObject(resourceDrop).FindProperty("type");
            if (typeProperty != null && typeProperty.enumValueIndex == 1)
            {
                foundAstralShard = true;
                Assert.AreEqual(LootItemType.Currency, item.type);
            }
        }

        Assert.IsTrue(foundAstralShard, "MobLoot must contain a configured Astral Shard currency entry.");
    }

    [Test]
    public void InputConfig_UsesConfiguredSpaceConfirmation()
    {
        InputConfig input = AssetDatabase.LoadAssetAtPath<InputConfig>("Assets/Data/Player/Input.asset");
        Assert.NotNull(input);
        Assert.AreEqual(KeyCode.Space, input.confirmKey);
    }
}
#endif
