using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LootTableGenerator
{
    private const string OUTPUT_FOLDER = "Assets/Data/LootTables";

    private struct EntryDef
    {
        public string itemName;
        public LootItemType type;
        public int tier;
        public float dropChance;
        public int minAmount;
        public int maxAmount;

        public EntryDef(string name, LootItemType type, int tier, float chance, int min, int max)
        {
            itemName   = name;
            this.type  = type;
            this.tier  = tier;
            dropChance = chance;
            minAmount  = min;
            maxAmount  = max;
        }
    }

    private struct TableDef
    {
        public string tableName;
        public EntryDef[] entries;
        public TableDef(string name, EntryDef[] entries) { tableName = name; this.entries = entries; }
    }

    private static readonly TableDef[] Tables = new TableDef[]
    {
        new TableDef("MobLoot", new EntryDef[]
        {
            new EntryDef("Echo Shards",      LootItemType.Currency,    1, 100f,  1,  5),
            new EntryDef("Memory Gold",      LootItemType.Currency,    2,  15f,  1,  3),
            new EntryDef("Lumin Orb",        LootItemType.Consumable,  1,   5f,  1,  1),
            new EntryDef("Basic Blaze Echo", LootItemType.Equipment,   1,   2f,  1,  1),
        }),

        new TableDef("CursedMobLoot", new EntryDef[]
        {
            new EntryDef("Echo Shards",      LootItemType.Currency,    1, 100f,  8, 15),
            new EntryDef("Memory Gold",      LootItemType.Currency,    2,  40f,  3,  8),
            new EntryDef("Lumin Orb",        LootItemType.Consumable,  1,  15f,  1,  1),
            new EntryDef("Basic Blaze Echo", LootItemType.Equipment,   1,  10f,  1,  1),
            new EntryDef("Broken Chains",    LootItemType.Relic,       1,   5f,  1,  1),
        }),

        new TableDef("EliteLoot", new EntryDef[]
        {
            new EntryDef("Echo Shards",          LootItemType.Currency,    1, 100f, 15, 30),
            new EntryDef("Memory Gold",          LootItemType.Currency,    2, 100f, 15, 25),
            new EntryDef("Lumin Orb",            LootItemType.Consumable,  1,  40f,  1,  2),
            new EntryDef("Basic Blaze Echo",     LootItemType.Equipment,   1,  25f,  1,  1),
            new EntryDef("Advanced Void Echo",   LootItemType.Equipment,   2,  15f,  1,  1),
            new EntryDef("Broken Chains",        LootItemType.Relic,       1,  25f,  1,  1),
            new EntryDef("Glitched Monolith",    LootItemType.Relic,       2,   8f,  1,  1),
        }),

        new TableDef("BasicChestLoot", new EntryDef[]
        {
            new EntryDef("Echo Shards",          LootItemType.Currency,    1, 100f, 20, 40),
            new EntryDef("Memory Gold",          LootItemType.Currency,    2, 100f, 20, 30),
            new EntryDef("Lumin Orb",            LootItemType.Consumable,  1,  50f,  1,  2),
            new EntryDef("Basic Blaze Echo",     LootItemType.Equipment,   1,  40f,  1,  1),
            new EntryDef("Advanced Void Echo",   LootItemType.Equipment,   2,   5f,  1,  1),
            new EntryDef("Broken Chains",        LootItemType.Relic,       1,  30f,  1,  1),
        }),

        new TableDef("UncommonChestLoot", new EntryDef[]
        {
            new EntryDef("Echo Shards",            LootItemType.Currency,    1, 100f, 35, 60),
            new EntryDef("Memory Gold",            LootItemType.Currency,    2, 100f, 30, 50),
            new EntryDef("Advanced Void Echo",     LootItemType.Equipment,   2,  30f,  1,  1),
            new EntryDef("Ultimate Curse Echo",    LootItemType.Equipment,   3,   5f,  1,  1),
            new EntryDef("Glitched Monolith",      LootItemType.Relic,       2,  20f,  1,  1),
        }),

        new TableDef("RareChestLoot", new EntryDef[]
        {
            new EntryDef("Echo Shards",            LootItemType.Currency,    1, 100f,  50, 100),
            new EntryDef("Memory Gold",            LootItemType.Currency,    2, 100f,  50,  80),
            new EntryDef("Advanced Void Echo",     LootItemType.Equipment,   2,  50f,   1,   1),
            new EntryDef("Ultimate Curse Echo",    LootItemType.Equipment,   3,  15f,   1,   1),
            new EntryDef("Glitched Monolith",      LootItemType.Relic,       2,  40f,   1,   1),
            new EntryDef("Phantom's Heart",        LootItemType.Relic,       3,  10f,   1,   1),
        }),

        new TableDef("CursedChestLoot", new EntryDef[]
        {
            new EntryDef("Echo Shards",          LootItemType.Currency,    1, 100f, 80, 120),
            new EntryDef("Ultimate Curse Echo",  LootItemType.Equipment,   3, 100f,  1,   1),
            new EntryDef("Phantom's Heart",      LootItemType.Relic,       3,  30f,  1,   1),
        }),
    };

    [MenuItem("Project Echoes/Generate Loot Tables")]
    private static void Generate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
            AssetDatabase.CreateFolder("Assets/Data", "LootTables");

        foreach (TableDef tableDef in Tables)
        {
            string assetPath = $"{OUTPUT_FOLDER}/{tableDef.tableName}.asset";

            LootTable asset = AssetDatabase.LoadAssetAtPath<LootTable>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LootTable>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            SerializedObject so = new SerializedObject(asset);
            SerializedProperty itemsProp = so.FindProperty("lootItems");
            itemsProp.ClearArray();

            for (int i = 0; i < tableDef.entries.Length; i++)
            {
                EntryDef def = tableDef.entries[i];
                itemsProp.InsertArrayElementAtIndex(i);
                SerializedProperty elem = itemsProp.GetArrayElementAtIndex(i);

                elem.FindPropertyRelative("itemPrefab").objectReferenceValue = null; // designer assigns later
                elem.FindPropertyRelative("dropChance").floatValue           = def.dropChance;
                elem.FindPropertyRelative("itemTier").intValue               = def.tier;
                elem.FindPropertyRelative("minAmount").intValue              = def.minAmount;
                elem.FindPropertyRelative("maxAmount").intValue              = def.maxAmount;
                elem.FindPropertyRelative("type").enumValueIndex             = (int)def.type;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LootTableGenerator] Generated {Tables.Length} Loot Table assets in '{OUTPUT_FOLDER}'.");
    }
}
