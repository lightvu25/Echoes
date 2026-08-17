#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class RelicEquipmentRuntimeValidator
{
    private static readonly HashSet<string> ExpectedRelics = new HashSet<string>
    {
        "GraftingFlask", "BloodthirstyMoss", "AcidicGallbladder", "DarkBandage", "ShatteredMemory",
        "RustyGrapple", "RottenWeb", "BurrowersScale", "ToxicSpore", "RustedCoin", "EmberFirefly",
        "SpikedCleats", "EchoWhetstone", "BouncerShroom", "VialOfAshes", "BatsTalon",
        "RustyHeavyChain", "DriedCyclopsEye", "VolatileCore", "BloodContract", "OreSparkCore",
        "StalactiteHeart", "SoulBell", "CondemnedRing", "EchoingSigil", "AbyssalTreads", "VampiricFang"
    };

    private static readonly HashSet<string> ExpectedTools = new HashSet<string>
    {
        "BOMB", "CRIMSON_DART", "ADRENALINE_VIAL", "ARACHNE_TRAP", "TOXIC_FLASK", "VOID_AEGIS", "KINETIC_ROOT"
    };

    [MenuItem("Tools/Echoes/Validate Relics and Equipment")]
    public static void ValidateAll()
    {
        int errors = 0;
        errors += ValidateAssets<RelicData>("Assets/Data/Relics", ExpectedRelics, PlayerRelicManager.SupportsRelic, "Relic");
        errors += ValidateAssets<ToolData>("Assets/Data/Equipments", ExpectedTools, EquipmentRuntimeRegistry.SupportsTool, "Equipment");
        errors += ValidatePlayerEquipmentWiring();
        errors += ValidateEliteMetadata();

        if (errors == 0)
            Debug.Log($"[Runtime Validator] PASS: {ExpectedRelics.Count} Relics and {ExpectedTools.Count} Equipment assets are registered and wired.");
        else
            Debug.LogError($"[Runtime Validator] FAIL: found {errors} Relic/Equipment configuration error(s). See messages above.");
    }

    private static int ValidateAssets<T>(string folder, HashSet<string> expectedIds,
        System.Func<string, bool> supports, string label) where T : ItemBaseData
    {
        int errors = 0;
        HashSet<string> found = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null || string.IsNullOrWhiteSpace(asset.itemID))
            {
                Debug.LogError($"[Runtime Validator] {label} asset at '{path}' has no itemID.", asset);
                errors++;
                continue;
            }
            if (!found.Add(asset.itemID))
            {
                Debug.LogError($"[Runtime Validator] Duplicate {label} itemID '{asset.itemID}'.", asset);
                errors++;
            }
            if (!supports(asset.itemID))
            {
                Debug.LogError($"[Runtime Validator] {label} '{asset.itemID}' has no runtime behavior.", asset);
                errors++;
            }
        }

        foreach (string expected in expectedIds)
        {
            if (found.Contains(expected)) continue;
            Debug.LogError($"[Runtime Validator] Missing expected {label} asset '{expected}' in {folder}.");
            errors++;
        }
        return errors;
    }

    private static int ValidatePlayerEquipmentWiring()
    {
        const string playerPath = "Assets/Prefabs/Entities/Player/Player.prefab";
        GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
        if (player == null)
        {
            Debug.LogError($"[Runtime Validator] Missing Player prefab at '{playerPath}'.");
            return 1;
        }

        int errors = 0;
        if (player.GetComponent<PlayerTool>() == null) { Debug.LogError("[Runtime Validator] Player prefab is missing PlayerTool.", player); errors++; }
        if (player.GetComponent<PlayerEventBus>() == null) { Debug.LogError("[Runtime Validator] Player prefab is missing PlayerEventBus.", player); errors++; }
        if (player.GetComponent<PlayerRelicManager>() == null) { Debug.LogError("[Runtime Validator] Player prefab is missing PlayerRelicManager.", player); errors++; }
        EquipmentRuntimeRegistry registry = player.GetComponent<EquipmentRuntimeRegistry>();
        if (registry == null) { Debug.LogError("[Runtime Validator] Player prefab is missing EquipmentRuntimeRegistry.", player); return errors + 1; }

        SerializedObject serializedRegistry = new SerializedObject(registry);
        string[] prefabFields =
        {
            "bloodOreBombPrefab", "crimsonDartPrefab", "arachneTrapPrefab",
            "toxicFlaskPrefab", "toxicCloudPrefab", "kineticRootPrefab"
        };
        foreach (string field in prefabFields)
        {
            SerializedProperty property = serializedRegistry.FindProperty(field);
            if (property == null || property.objectReferenceValue != null) continue;
            Debug.LogError($"[Runtime Validator] EquipmentRuntimeRegistry.{field} is not assigned.", registry);
            errors++;
        }
        return errors;
    }

    private static int ValidateEliteMetadata()
    {
        const string elitePath = "Assets/Prefabs/Entities/Enemy/Elite Enemy.prefab";
        GameObject elite = AssetDatabase.LoadAssetAtPath<GameObject>(elitePath);
        EnemyCombat combat = elite != null ? elite.GetComponent<EnemyCombat>() : null;
        if (combat != null && combat.Rank == EnemyRank.Elite) return 0;
        Debug.LogError($"[Runtime Validator] Elite Enemy prefab must have EnemyCombat rank set to Elite.", elite);
        return 1;
    }
}
#endif
