using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MindGardenNodeDataGenerator
{
    private const string OUTPUT_FOLDER = "Assets/Constellation";

    private struct NodeDef
    {
        public string skillID;
        public string skillName;
        public int    tier;
        public string description;
        public int    memoryCost;
        public string prerequisiteID; // empty = no prerequisite

        public NodeDef(string id, string name, int tier, string desc, int cost, string prereq = "")
        {
            skillID        = id;
            skillName      = name;
            this.tier      = tier;
            description    = desc;
            memoryCost     = cost;
            prerequisiteID = prereq;
        }
    }

    private static readonly NodeDef[] Nodes = new NodeDef[]
    {
        // Tier 1
        new NodeDef("UNLOCK_FLASK_1",    "Lumin Flask I",         1, "Allows you to carry 2 Lumin Flask per run. Healing power slightly improved to 45% HP.",                                        50,  ""),
        new NodeDef("GOLD_RESERVE_1",    "Memory Vault I",        1, "Give you 100 Gold to carry over directly into your run.",                                                                      50,  ""),
        new NodeDef("UNLOCK_RECYCLE_1",  "Alchemist's Pouch I",   1, "Dismantle unused Echoes into Gold at 10% of their shop value.",                                                               50,  ""),
        new NodeDef("PHANTOM_LOOT_1",    "Phantom's Legacy I",    1, "Recovers 25% of the Shards/Gold you lost.",                                                                                   50,  ""),
        new NodeDef("SHRINE_REROLL",     "Fate's Vision",         1, "Blessing Shrines now have a \"Reroll Cards\" button costing 50 Gold.",                                                         50,  ""),

        // Tier 2
        new NodeDef("UNLOCK_FLASK_2",    "Lumin Flask II",        2, "Increases your maximum Lumin Flask carrying capacity to 3. Healing power slightly improved to 50% HP.",                       150, "UNLOCK_FLASK_1"),
        new NodeDef("STARTING_ECHO_1",   "Genesis Remnant I",     2, "Begin a new run with a choice of 1 random Basic Echo (Tier I) from a pedestal.",                                             150, "UNLOCK_FLASK_1"),
        new NodeDef("GOLD_RESERVE_2",    "Memory Vault II",       2, "Retain up to 2,500 Gold after death.",                                                                                        150, "GOLD_RESERVE_1"),
        new NodeDef("UNLOCK_RECYCLE_2",  "Alchemist's Pouch II",  2, "Dismantling Echoes now yields 30% of their shop value.",                                                                      250, "UNLOCK_RECYCLE_1"),
        new NodeDef("FUSION_TIER_2",     "Advanced Forge I",      2, "The Fusion Altar can now combine Echoes with Tier II Fusions.",                                                               250, "STARTING_ECHO_1"),
        new NodeDef("PHANTOM_LOOT_2",    "Phantom's Legacy II",   2, "Claim 50% of your lost loot and a random Echo you had equipped.",                                                             300, "PHANTOM_LOOT_1"),

        // Tier 3
        new NodeDef("UNFUSION",          "Unfused Glove",         3, "Unfuse the Echo Fusion to its 1 random Echo.",                                                                                400, "UNLOCK_RECYCLE_2"),
        new NodeDef("UNLOCK_FLASK_3",    "Lumin Flask III",       3, "Increases capacity to 4. Healing power slightly improved to 55% HP.",                                                         400, "UNLOCK_FLASK_2"),
        new NodeDef("ECHO_CAPACITY_UP",  "Expanded Core",         3, "Increases the maximum number of Echoes you can equip simultaneously by 1.",                                                   500, "FUSION_TIER_3"),
        new NodeDef("PHANTOM_LOOT_3",    "Phantom's Legacy III",  3, "Claim 75% of your lost loot and 1 or 2 random Echo you had equipped.",                                                        500, "PHANTOM_LOOT_2"),
        new NodeDef("FUSION_TIER_3",     "Advanced Forge II",     3, "The Fusion Altar can now combine Echoes with Tier III Fusions.",                                                              500, "FUSION_TIER_2"),

        // Tier 4
        new NodeDef("UNLOCK_FLASK_4",    "Lumin Flask IV",        4, "Increases maximum capacity to 5. Healing power slightly improved to 60% HP.",                                                 700, "UNLOCK_FLASK_3"),
        new NodeDef("PHANTOM_LOOT_4",    "Perfect Resonance",     4, "Phantom now drops the exact Fusion you were wielding when you died.",                                                          700, "PHANTOM_LOOT_2"),
    };

    [MenuItem("Project Echoes/Generate Constellation Data")]
    private static void Generate()
    {
        // --- Ensure output folder ---
        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
        {
            AssetDatabase.CreateFolder("Assets", "Constellation");
            Debug.Log($"[MindGardenNodeDataGenerator] Created folder: {OUTPUT_FOLDER}");
        }

        // --- Pass 1: Create assets and set scalar fields ---
        var createdAssets = new Dictionary<string, MindGardenNodeData>();

        foreach (NodeDef def in Nodes)
        {
            string assetPath = $"{OUTPUT_FOLDER}/{def.skillID}.asset";

            // Reuse existing asset to avoid destroying references
            MindGardenNodeData asset = AssetDatabase.LoadAssetAtPath<MindGardenNodeData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MindGardenNodeData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_skillID").stringValue    = def.skillID;
            so.FindProperty("_skillName").stringValue  = def.skillName;
            so.FindProperty("_tier").intValue          = def.tier;
            so.FindProperty("_description").stringValue = def.description;
            so.FindProperty("_memoryCost").intValue    = def.memoryCost;

            // Clear prerequisites now; they are wired in Pass 2
            so.FindProperty("_prerequisites").ClearArray();

            so.ApplyModifiedPropertiesWithoutUndo();
            createdAssets[def.skillID] = asset;
        }

        AssetDatabase.SaveAssets();

        // --- Pass 2: Wire prerequisites ---
        foreach (NodeDef def in Nodes)
        {
            if (string.IsNullOrEmpty(def.prerequisiteID)) continue;

            if (!createdAssets.TryGetValue(def.skillID, out MindGardenNodeData asset)) continue;
            if (!createdAssets.TryGetValue(def.prerequisiteID, out MindGardenNodeData prereqAsset))
            {
                Debug.LogWarning($"[MindGardenNodeDataGenerator] Prerequisite '{def.prerequisiteID}' not found for '{def.skillID}'.");
                continue;
            }

            SerializedObject so    = new SerializedObject(asset);
            SerializedProperty list = so.FindProperty("_prerequisites");

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = prereqAsset;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MindGardenNodeDataGenerator] Successfully generated {Nodes.Length} Constellation assets in '{OUTPUT_FOLDER}'.");
    }
}
