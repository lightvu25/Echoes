#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates or updates every Fusion Echo and its recipe from one typed source of truth.
/// Existing assets are updated in place so icons, VFX references, GUIDs, and scene
/// references are preserved.
/// </summary>
public static class FusionDataGenerator
{
    private const string BasicEchoFolder = "Assets/Data/Echo/Basic";
    private const string FusionEchoFolder = "Assets/Data/Echo/Fusion";
    private const string FusionRecipeFolder = "Assets/Data/Echo/Fusion Recipe";
    private const string LegacyRecipeFolder = "Assets/Data/Echo/Fusion/Recipes";
    private const string FusionIconFolder = "Assets/Sprites/UI/Echo/Fusion";
    private const string DefaultDropPrefabPath = "Assets/Prefabs/Items/Drop/Item Drop.prefab";
    private const string ChainLightningPrefabPath = "Assets/Prefabs/VFX/Echo Effect/ChainLightningVFX.prefab";

    private static readonly string[] LegacyDuplicateResultPaths =
    {
        "Assets/Data/Echo/Fusion/Tier II/Cryo Stasis.asset",
        "Assets/Data/Echo/Fusion/Tier II/Death Drive.asset"
    };

    private readonly struct FusionDefinition
    {
        public readonly string ModifierID;
        public readonly string ItemID;
        public readonly string ComponentA;
        public readonly string ComponentB;
        public readonly string DisplayName;
        public readonly string ResultFileName;
        public readonly string RecipeFileName;
        public readonly int Tier;
        public readonly string Description;
        public readonly float DamageMultiplier;
        public readonly float AttacksPerSecond;
        public readonly EchoRange Range;
        public readonly float StatusProcCoefficient;

        public FusionDefinition(
            string modifierID,
            string componentA,
            string componentB,
            string displayName,
            string resultFileName,
            string recipeFileName,
            int tier,
            string description,
            float damageMultiplier,
            float attacksPerSecond,
            EchoRange range,
            float statusProcCoefficient)
        {
            ModifierID = modifierID;
            ItemID = "ECHO_" + modifierID.Substring("FUS_".Length);
            ComponentA = componentA;
            ComponentB = componentB;
            DisplayName = displayName;
            ResultFileName = resultFileName;
            RecipeFileName = recipeFileName;
            Tier = tier;
            Description = description;
            DamageMultiplier = damageMultiplier;
            AttacksPerSecond = attacksPerSecond;
            Range = range;
            StatusProcCoefficient = statusProcCoefficient;
        }

        public string TierName => Tier switch
        {
            1 => "Tier I",
            2 => "Tier II",
            3 => "Tier III",
            _ => throw new ArgumentOutOfRangeException(nameof(Tier), Tier, "Fusion tier must be between 1 and 3.")
        };

        public string RequiredConstellationNode => Tier switch
        {
            1 => string.Empty,
            2 => "FUSION_TIER_2",
            3 => "FUSION_TIER_3",
            _ => string.Empty
        };
    }

    private readonly struct ResolvedComponents
    {
        public readonly EchoData EchoA;
        public readonly EchoData EchoB;

        public ResolvedComponents(EchoData echoA, EchoData echoB)
        {
            EchoA = echoA;
            EchoB = echoB;
        }
    }

    private static readonly FusionDefinition[] Definitions =
    {
        new FusionDefinition(
            "FUS_PLASMA", "ECHO_BLAZE", "ECHO_ARC", "Plasma", "Plasma.asset", "Plasma Fusion.asset", 1,
            "Hits launch heated lightning to nearby enemies. Every arc impact explodes for 45% attack damage and applies Burn.",
            1.10f, 1.20f, EchoRange.Ranged, 0.50f),

        new FusionDefinition(
            "FUS_AVALANCHE", "ECHO_FROSTBITE", "ECHO_KINETIC", "Avalanche", "Avalanche.asset", "Avalanche Fusion.asset", 1,
            "Hits add Fracture to slowed or frozen enemies. At 3 stacks they shatter for 180% area damage; frozen targets shatter immediately.",
            1.35f, 0.85f, EchoRange.Melee, 0.65f),

        new FusionDefinition(
            "FUS_AFTERBURNER", "ECHO_KINETIC", "ECHO_BLAZE", "Afterburner", "Afterburner.asset", "Afterburner Fusion.asset", 1,
            "Every third successful hit becomes Overdriven, dealing 75% bonus damage and creating a burning line through the target.",
            1.25f, 1.00f, EchoRange.Hybrid, 0.60f),

        new FusionDefinition(
            "FUS_ENTROPY", "ECHO_CURSE", "ECHO_ANOMALY", "Entropy", "Entropy.asset", "Entropy.asset", 1,
            "All hits deal True Damage and roll between 60% and 200% power. Low rolls cause recoil; high rolls create an echo explosion.",
            1.00f, 1.00f, EchoRange.Hybrid, 1.00f),

        new FusionDefinition(
            "FUS_OVERCLOCK", "ECHO_CURSE", "ECHO_ARC", "Overclock", "Overclock.asset", "Overclock.asset", 1,
            "Consecutive hits build Voltage and increase damage. At 5 stacks, discharge powerful chain damage and suffer Max HP recoil.",
            1.00f, 1.15f, EchoRange.Ranged, 0.50f),

        new FusionDefinition(
            "FUS_NEON_GRID", "ECHO_ANOMALY", "FUS_PLASMA", "Neon Grid", "Neon Grid.asset", "Neon Grid Fusion.asset", 2,
            "Plasma impacts leave pulsing Glitch Nodes. Hitting a node links nearby nodes and detonates each for heavy damage.",
            1.25f, 1.00f, EchoRange.Ranged, 0.75f),

        new FusionDefinition(
            "FUS_SUPERNOVA", "ECHO_VOID", "FUS_AFTERBURNER", "Supernova", "Supernova.asset", "Supernova Fusion.asset", 2,
            "Every third hit implants an Oblivion Core. Hitting it again pulls nearby enemies inward and detonates for massive damage plus Burn.",
            1.50f, 0.80f, EchoRange.Mid, 0.75f),

        new FusionDefinition(
            "FUS_DEATH_DRIVE", "ECHO_CURSE", "FUS_AVALANCHE", "Death-Drive", "Death-Drive.asset", "Death-Drive Fusion.asset", 2,
            "Damage increases as HP falls, up to 100%. Shattering a fractured or frozen target at low HP releases a violent area crash.",
            1.35f, 0.95f, EchoRange.Melee, 0.75f),

        new FusionDefinition(
            "FUS_CRYO_STASIS", "ECHO_VOID", "FUS_AVALANCHE", "Cryo-Stasis", "Cryo-Stasis.asset", "Cryo-Stasis Fusion.asset", 2,
            "Hits add Stasis. At 3 stacks the target freezes; the next hit shatters it for massive True Damage and damages nearby enemies.",
            1.25f, 0.85f, EchoRange.Mid, 1.00f),

        new FusionDefinition(
            "FUS_EVENT_HORIZON", "FUS_SUPERNOVA", "FUS_NEON_GRID", "Event Horizon", "Event Horizon.asset", "Event Horizon Fusion.asset", 3,
            "Every fifth hit creates a singularity that pulls enemies, deals repeated True Damage, then collapses and detonates active marks.",
            1.60f, 0.80f, EchoRange.Ranged, 1.00f),

        new FusionDefinition(
            "FUS_RAGNAROK", "FUS_OVERCLOCK", "FUS_DEATH_DRIVE", "Ragnarok", "Ragnarok.asset", "Ragnarok Fusion.asset", 3,
            "Max HP is locked to one segment and attacks deal devastating damage. Every fifth hit releases a shatter wave; taking damage is fatal.",
            1.00f, 1.00f, EchoRange.Melee, 1.00f),

        new FusionDefinition(
            "FUS_ZERO_POINT", "FUS_ENTROPY", "FUS_CRYO_STASIS", "Zero Point", "Zero Point.asset", "Zero Point Fusion.asset", 3,
            "Every fourth hit creates a Zero Field. Frozen-target damage becomes True Damage and echoes to every other frozen enemy before collapse.",
            1.50f, 1.00f, EchoRange.Ranged, 1.00f)
    };

    [MenuItem("Tools/Echoes/Generate Fusion Data")]
    public static void GenerateFusionData()
    {
        EnsureFolderHierarchy();

        if (!TryPreflightDefinitions(out Dictionary<string, EchoData> echoesByKey))
        {
            Debug.LogError("[FusionDataGenerator] Generation aborted during preflight. No canonical assets were modified.");
            return;
        }

        QuarantineKnownLegacyDuplicates();

        Dictionary<string, EchoData> generatedResults = new Dictionary<string, EchoData>(StringComparer.Ordinal);
        int createdResults = 0;
        int updatedResults = 0;

        // Pass 1 creates every result so higher-tier recipes can reference lower-tier Fusions.
        foreach (FusionDefinition definition in Definitions)
        {
            string resultPath = GetResultPath(definition);
            EchoData result = AssetDatabase.LoadAssetAtPath<EchoData>(resultPath);
            if (result == null)
            {
                result = ScriptableObject.CreateInstance<EchoData>();
                AssetDatabase.CreateAsset(result, resultPath);
                createdResults++;
            }
            else
            {
                updatedResults++;
            }

            PopulateResult(result, definition);
            generatedResults[definition.ModifierID] = result;
            echoesByKey[definition.ModifierID] = result;
            echoesByKey[definition.ItemID] = result;
        }

        Dictionary<string, ResolvedComponents> resolvedComponents = new Dictionary<string, ResolvedComponents>(StringComparer.Ordinal);
        bool allComponentsResolved = true;

        foreach (FusionDefinition definition in Definitions)
        {
            EchoData echoA = ResolveEcho(echoesByKey, definition.ComponentA, definition.ModifierID);
            EchoData echoB = ResolveEcho(echoesByKey, definition.ComponentB, definition.ModifierID);
            if (echoA == null || echoB == null)
            {
                allComponentsResolved = false;
                continue;
            }

            resolvedComponents[definition.ModifierID] = new ResolvedComponents(echoA, echoB);
        }

        if (!allComponentsResolved)
        {
            Debug.LogError("[FusionDataGenerator] Generation aborted before recipe mutation because one or more components could not be resolved.");
            return;
        }

        AssetDatabase.SaveAssets();

        int createdRecipes = 0;
        int updatedRecipes = 0;

        // Pass 2 wires recipes after every result is available.
        foreach (FusionDefinition definition in Definitions)
        {
            string recipePath = GetRecipePath(definition);
            FusionRecipeData recipe = AssetDatabase.LoadAssetAtPath<FusionRecipeData>(recipePath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<FusionRecipeData>();
                AssetDatabase.CreateAsset(recipe, recipePath);
                createdRecipes++;
            }
            else
            {
                updatedRecipes++;
            }

            ResolvedComponents components = resolvedComponents[definition.ModifierID];
            recipe.recipeID = definition.ModifierID;
            recipe.echoA = components.EchoA;
            recipe.echoB = components.EchoB;
            recipe.resultEcho = generatedResults[definition.ModifierID];
            recipe.recipeTier = definition.Tier;
            recipe.requiredConstellationNode = definition.RequiredConstellationNode;
            EditorUtility.SetDirty(recipe);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bool isValid = ValidateFusionData(logSuccess: false);
        string status = isValid ? "Validation passed." : "Validation failed; see Console errors.";
        Debug.Log(
            $"[FusionDataGenerator] Complete. Results created: {createdResults}, updated: {updatedResults}. " +
            $"Recipes created: {createdRecipes}, updated: {updatedRecipes}. {status}");
    }

    [MenuItem("Tools/Echoes/Validate Fusion Data")]
    public static void ValidateFusionDataMenu()
    {
        ValidateFusionData(logSuccess: true);
    }

    public static bool ValidateFusionData(bool logSuccess)
    {
        bool valid = true;
        HashSet<string> itemIDs = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> recipeIDs = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, EchoData> expectedEchoes = new Dictionary<string, EchoData>(StringComparer.Ordinal);

        if (!TryBuildBasicEchoLookup(expectedEchoes)) valid = false;

        foreach (FusionDefinition definition in Definitions)
        {
            EchoData result = AssetDatabase.LoadAssetAtPath<EchoData>(GetResultPath(definition));
            if (result == null) continue;
            expectedEchoes[definition.ModifierID] = result;
            expectedEchoes[definition.ItemID] = result;
        }

        foreach (FusionDefinition definition in Definitions)
        {
            EchoData result = AssetDatabase.LoadAssetAtPath<EchoData>(GetResultPath(definition));
            FusionRecipeData recipe = AssetDatabase.LoadAssetAtPath<FusionRecipeData>(GetRecipePath(definition));

            if (result == null)
            {
                Debug.LogError($"[FusionDataGenerator] Missing result asset for {definition.DisplayName}.");
                valid = false;
            }
            else
            {
                if (result.itemID != definition.ItemID || result.uniqueModifierID != definition.ModifierID)
                {
                    Debug.LogError($"[FusionDataGenerator] Result identity mismatch for {definition.DisplayName}.", result);
                    valid = false;
                }

                if (result.itemName != definition.DisplayName ||
                    result.description != definition.Description ||
                    result.itemTier != definition.Tier ||
                    result.basePrice != GetBasePrice(definition.Tier) ||
                    result.echoType != EchoType.Composite ||
                    !result.isFusionResult ||
                    !Mathf.Approximately(result.baseDamageMultiplier, definition.DamageMultiplier) ||
                    !Mathf.Approximately(result.attacksPerSecond, definition.AttacksPerSecond) ||
                    result.rangeCategory != definition.Range ||
                    !result.hasStatusEffect ||
                    !Mathf.Approximately(result.statusProcCoefficient, definition.StatusProcCoefficient))
                {
                    Debug.LogError($"[FusionDataGenerator] Generated result fields do not match the definition for {definition.DisplayName}.", result);
                    valid = false;
                }

                if (result.itemIcon == null || result.dropPrefab == null)
                {
                    Debug.LogError($"[FusionDataGenerator] Required icon or drop prefab is missing for {definition.DisplayName}.", result);
                    valid = false;
                }

                if (definition.ModifierID == "FUS_PLASMA" && result.chainLightningVFXPrefab == null)
                {
                    Debug.LogError("[FusionDataGenerator] Plasma requires the Chain Lightning VFX prefab.", result);
                    valid = false;
                }

                if (!itemIDs.Add(result.itemID))
                {
                    Debug.LogError($"[FusionDataGenerator] Duplicate generated item ID: {result.itemID}.", result);
                    valid = false;
                }
            }

            if (recipe == null)
            {
                Debug.LogError($"[FusionDataGenerator] Missing recipe asset for {definition.DisplayName}.");
                valid = false;
                continue;
            }

            expectedEchoes.TryGetValue(definition.ComponentA, out EchoData expectedA);
            expectedEchoes.TryGetValue(definition.ComponentB, out EchoData expectedB);

            if (recipe.echoA == null || recipe.echoB == null || recipe.resultEcho == null)
            {
                Debug.LogError($"[FusionDataGenerator] Recipe '{definition.DisplayName}' has missing references.", recipe);
                valid = false;
            }

            if (recipe.recipeID != definition.ModifierID || recipe.echoA != expectedA || recipe.echoB != expectedB ||
                recipe.resultEcho != result || recipe.recipeTier != definition.Tier ||
                recipe.requiredConstellationNode != definition.RequiredConstellationNode)
            {
                Debug.LogError($"[FusionDataGenerator] Recipe metadata mismatch for {definition.DisplayName}.", recipe);
                valid = false;
            }

            if (!recipeIDs.Add(recipe.recipeID))
            {
                Debug.LogError($"[FusionDataGenerator] Duplicate generated recipe ID: {recipe.recipeID}.", recipe);
                valid = false;
            }
        }

        if (!ValidateGlobalIdentityUniqueness()) valid = false;

        if (valid && logSuccess)
        {
            Debug.Log($"[FusionDataGenerator] Validation passed for all {Definitions.Length} Fusion results and recipes.");
        }

        return valid;
    }

    private static void PopulateResult(EchoData result, FusionDefinition definition)
    {
        result.itemID = definition.ItemID;
        result.itemName = definition.DisplayName;
        result.description = definition.Description;
        if (result.itemIcon == null) result.itemIcon = LoadFusionIcon(definition.DisplayName);
        if (result.dropPrefab == null) result.dropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultDropPrefabPath);
        result.itemTier = definition.Tier;
        result.basePrice = GetBasePrice(definition.Tier);
        result.echoType = EchoType.Composite;
        result.isFusionResult = true;
        result.baseDamageMultiplier = definition.DamageMultiplier;
        result.attacksPerSecond = definition.AttacksPerSecond;
        result.rangeCategory = definition.Range;
        result.hasStatusEffect = true;
        result.statusProcCoefficient = definition.StatusProcCoefficient;
        result.uniqueModifierID = definition.ModifierID;
        if (definition.ModifierID == "FUS_PLASMA" && result.chainLightningVFXPrefab == null)
            result.chainLightningVFXPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChainLightningPrefabPath);
        EditorUtility.SetDirty(result);
    }

    private static Sprite LoadFusionIcon(string displayName)
    {
        foreach (string extension in new[] { ".aseprite", ".png" })
        {
            string path = $"{FusionIconFolder}/{displayName}{extension}";
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite) return sprite;
            }
        }

        Debug.LogError($"[FusionDataGenerator] Could not resolve a Fusion icon for '{displayName}'.");
        return null;
    }

    private static bool TryPreflightDefinitions(out Dictionary<string, EchoData> echoes)
    {
        echoes = new Dictionary<string, EchoData>(StringComparer.Ordinal);
        bool valid = TryBuildBasicEchoLookup(echoes);
        HashSet<string> modifierIDs = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> itemIDs = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> resultPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> recipePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (FusionDefinition definition in Definitions)
        {
            if (!modifierIDs.Add(definition.ModifierID) || !itemIDs.Add(definition.ItemID) ||
                !resultPaths.Add(GetResultPath(definition)) || !recipePaths.Add(GetRecipePath(definition)))
            {
                Debug.LogError($"[FusionDataGenerator] Duplicate definition identity or path detected for '{definition.DisplayName}'.");
                valid = false;
            }
        }

        foreach (FusionDefinition definition in Definitions)
        {
            if (!IsResolvableDefinitionKey(definition.ComponentA, echoes, modifierIDs))
            {
                Debug.LogError($"[FusionDataGenerator] Unknown component '{definition.ComponentA}' in '{definition.ModifierID}'.");
                valid = false;
            }

            if (!IsResolvableDefinitionKey(definition.ComponentB, echoes, modifierIDs))
            {
                Debug.LogError($"[FusionDataGenerator] Unknown component '{definition.ComponentB}' in '{definition.ModifierID}'.");
                valid = false;
            }
        }


        foreach (string basicKey in echoes.Keys)
        {
            if (modifierIDs.Contains(basicKey) || itemIDs.Contains(basicKey))
            {
                Debug.LogError($"[FusionDataGenerator] Basic Echo key '{basicKey}' collides with a reserved Fusion identity.");
                valid = false;
            }
        }

        return valid;
    }

    private static bool TryBuildBasicEchoLookup(Dictionary<string, EchoData> echoes)
    {
        bool valid = true;
        string[] guids = AssetDatabase.FindAssets("t:EchoData", new[] { BasicEchoFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EchoData echo = AssetDatabase.LoadAssetAtPath<EchoData>(path);
            if (echo == null) continue;

            valid &= TryAddUniqueEchoKey(echoes, echo.itemID, echo, path);
            valid &= TryAddUniqueEchoKey(echoes, echo.uniqueModifierID, echo, path);
        }

        return valid;
    }

    private static bool TryAddUniqueEchoKey(Dictionary<string, EchoData> echoes, string key, EchoData echo, string path)
    {
        if (string.IsNullOrEmpty(key)) return true;
        if (!echoes.TryGetValue(key, out EchoData existing))
        {
            echoes.Add(key, echo);
            return true;
        }

        string existingPath = AssetDatabase.GetAssetPath(existing);
        Debug.LogError($"[FusionDataGenerator] Duplicate basic Echo key '{key}' in '{existingPath}' and '{path}'.");
        return false;
    }

    private static bool IsResolvableDefinitionKey(
        string key,
        Dictionary<string, EchoData> basicEchoes,
        HashSet<string> fusionModifierIDs)
    {
        return basicEchoes.ContainsKey(key) || fusionModifierIDs.Contains(key);
    }

    private static EchoData ResolveEcho(Dictionary<string, EchoData> echoes, string key, string ownerModifierID)
    {
        if (echoes.TryGetValue(key, out EchoData echo) && echo != null) return echo;

        Debug.LogError($"[FusionDataGenerator] Could not resolve component '{key}' for recipe '{ownerModifierID}'.");
        return null;
    }

    private static void QuarantineKnownLegacyDuplicates()
    {
        bool changed = false;

        if (AssetDatabase.IsValidFolder(LegacyRecipeFolder))
        {
            string[] recipeGuids = AssetDatabase.FindAssets("t:FusionRecipeData", new[] { LegacyRecipeFolder });
            foreach (string guid in recipeGuids)
            {
                FusionRecipeData recipe = AssetDatabase.LoadAssetAtPath<FusionRecipeData>(AssetDatabase.GUIDToAssetPath(guid));
                if (recipe == null) continue;

                bool recipeChanged = false;
                if (!string.IsNullOrEmpty(recipe.recipeID) && !recipe.recipeID.StartsWith("LEGACY_", StringComparison.Ordinal))
                {
                    recipe.recipeID = "LEGACY_" + recipe.recipeID;
                    recipeChanged = true;
                }

                if (recipe.echoA != null || recipe.echoB != null || recipe.resultEcho != null ||
                    recipe.recipeTier != 0 || !string.IsNullOrEmpty(recipe.requiredConstellationNode))
                {
                    recipe.echoA = null;
                    recipe.echoB = null;
                    recipe.resultEcho = null;
                    recipe.recipeTier = 0;
                    recipe.requiredConstellationNode = string.Empty;
                    recipeChanged = true;
                }

                if (recipeChanged)
                {
                    EditorUtility.SetDirty(recipe);
                    changed = true;
                }
            }
        }

        foreach (string path in LegacyDuplicateResultPaths)
        {
            EchoData echo = AssetDatabase.LoadAssetAtPath<EchoData>(path);
            if (echo == null) continue;

            if (!string.IsNullOrEmpty(echo.itemID) && !echo.itemID.StartsWith("LEGACY_", StringComparison.Ordinal))
            {
                echo.itemID = "LEGACY_" + echo.itemID;
                changed = true;
            }

            if (!string.IsNullOrEmpty(echo.uniqueModifierID) && !echo.uniqueModifierID.StartsWith("LEGACY_", StringComparison.Ordinal))
            {
                echo.uniqueModifierID = "LEGACY_" + echo.uniqueModifierID;
                changed = true;
            }

            if (string.IsNullOrEmpty(echo.description) ||
                !echo.description.StartsWith("[Legacy duplicate]", StringComparison.Ordinal))
            {
                echo.description = "[Legacy duplicate] " + echo.description;
                changed = true;
            }

            if (changed) EditorUtility.SetDirty(echo);
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[FusionDataGenerator] Quarantined obsolete duplicate Fusion assets with LEGACY_ identities. Files were preserved for recovery.");
        }
    }

    private static bool ValidateGlobalIdentityUniqueness()
    {
        bool valid = true;
        Dictionary<string, string> itemPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> modifierPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        Dictionary<string, string> recipePaths = new Dictionary<string, string>(StringComparer.Ordinal);

        string[] echoGuids = AssetDatabase.FindAssets("t:EchoData", new[] { "Assets/Data/Echo" });
        foreach (string guid in echoGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EchoData echo = AssetDatabase.LoadAssetAtPath<EchoData>(path);
            if (echo == null) continue;

            valid &= RegisterGlobalIdentity(itemPaths, echo.itemID, path, "item ID");
            valid &= RegisterGlobalIdentity(modifierPaths, echo.uniqueModifierID, path, "modifier ID");
        }

        string[] recipeGuids = AssetDatabase.FindAssets("t:FusionRecipeData", new[] { "Assets/Data/Echo" });
        foreach (string guid in recipeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FusionRecipeData recipe = AssetDatabase.LoadAssetAtPath<FusionRecipeData>(path);
            if (recipe == null) continue;
            valid &= RegisterGlobalIdentity(recipePaths, recipe.recipeID, path, "recipe ID");
        }

        return valid;
    }

    private static bool RegisterGlobalIdentity(
        Dictionary<string, string> pathsByID,
        string id,
        string path,
        string identityType)
    {
        if (string.IsNullOrEmpty(id)) return true;
        if (!pathsByID.TryGetValue(id, out string existingPath))
        {
            pathsByID.Add(id, path);
            return true;
        }

        Debug.LogError($"[FusionDataGenerator] Duplicate {identityType} '{id}' in '{existingPath}' and '{path}'.");
        return false;
    }

    private static string GetResultPath(FusionDefinition definition)
    {
        return $"{FusionEchoFolder}/{definition.TierName}/{definition.ResultFileName}";
    }

    private static int GetBasePrice(int tier)
    {
        return tier switch { 1 => 75, 2 => 125, 3 => 200, _ => 50 };
    }

    private static string GetRecipePath(FusionDefinition definition)
    {
        return $"{FusionRecipeFolder}/{definition.TierName}/{definition.RecipeFileName}";
    }

    private static void EnsureFolderHierarchy()
    {
        EnsureFolder(FusionEchoFolder);
        EnsureFolder(FusionRecipeFolder);

        foreach (string tierName in new[] { "Tier I", "Tier II", "Tier III" })
        {
            EnsureFolder($"{FusionEchoFolder}/{tierName}");
            EnsureFolder($"{FusionRecipeFolder}/{tierName}");
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
        {
            throw new InvalidOperationException($"Cannot create invalid asset folder '{path}'.");
        }

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
