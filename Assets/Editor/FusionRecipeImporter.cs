using UnityEngine;
using UnityEditor;
using System.IO;

public class FusionRecipeImporter : EditorWindow
{
    private static string tsvData = @"FUS_PLASMA	Blaze	Arc	Plasma	I	Chain-lightning carries heat, triggering AoE explosions each time it bounces.	None	1.0x	1.3/s	Ranged
FUS_AVALANCHE	Frostbite	Kinetic	Avalanche	I	Slowed enemies shatter for massive bonus DMG if knocked into walls or other units.	None	1.4x	0.7/s	Melee
FUS_AFTERBURNER	Kinetic	Blaze	Afterburner	I	Dashes pierce enemies and leave a trail of fire that burns targets over time.	None	1.3x	0.8/s	Melee
FUS_ENTROPY	Curse	Anomaly	Entropy	I	100% True DMG. Trade-off: Weapon hitbox scales randomly per swing.	None	1.5x	1.2/s	Hybrid
FUS_OVERCLOCK	Curse	Arc	Overclock	I	Base ATK Speed x3. Trade-off: Constant HP drain and disables all healing.	None	0.8x	3.0/s	Ranged
FUS_NEON_GRID	Anomaly	FUS_PLASMA	Neon Grid	II	Explosive chain-lightning leaves glitched zones that disable enemy skills.	Anomaly + FUS_PLASMA	1.0x	1.4/s	Ranged
FUS_SUPERNOVA	Void	FUS_AFTERBURNER	Supernova	II	Vacuum pulls enemies into the blazing trail before detonating the Oblivion mark.	Void + FUS_AFTERBURNER	1.8x	0.6/s	Mid
FUS_DEATH_DRIVE	Curse	FUS_AVALANCHE	Death-Drive	II	Inverse HP scaling: The lower your HP, the stronger the knockback and shatter DMG.	Curse + FUS_AVALANCHE	2.5x	0.9/s	Melee
FUS_CRYO_STASIS	Void	FUS_AVALANCHE	Cryo-Stasis	II	Replaces knockback with an instant freeze. Shattering the frozen target executes them.	Void + FUS_AVALANCHE	1.2x	0.8/s	Mid
FUS_EVENT_HORIZON	FUS_SUPERNOVA	FUS_NEON_GRID	Event Horizon	III	Creates a massive black hole that sucks enemies into a glitched lava pit, dealing True DMG over time.	FUS_SUPERNOVA + FUS_NEON_GRID	2.0x	1.0/s	Ranged
FUS_RAGNAROK	FUS_OVERCLOCK	FUS_DEATH_DRIVE	Ragnarok	III	Ultimate Berserker state. Insane attack speed & shatter damage. Trade-off: HP is locked at 1. Any hit is fatal.	FUS_OVERCLOCK + FUS_DEATH_DRIVE	3.0x	2.5/s	Melee
FUS_ZERO_POINT	FUS_ENTROPY	FUS_CRYO_STASIS	Zero Point	III	Glitches the entire screen. Frozen enemies take True DMG and their freeze timers never run out until shattered.	FUS_ENTROPY + FUS_CRYO_STASIS	1.5x	1.0/s	Ranged";

    [MenuItem("Tools/Import Fusion Recipes")]
    public static void ImportRecipes()
    {
        string[] lines = tsvData.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        string fusionDir = "Assets/Data/Echo/Fusion";
        string recipesDir = "Assets/Data/Echo/Fusion/Recipes";

        if (!AssetDatabase.IsValidFolder("Assets/Data/Echo/Fusion")) AssetDatabase.CreateFolder("Assets/Data/Echo", "Fusion");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Echo/Fusion/Recipes")) AssetDatabase.CreateFolder("Assets/Data/Echo/Fusion", "Recipes");

        foreach (string line in lines)
        {
            if (line.Trim().StartsWith("Recipe ID")) continue; // Skip header if present

            string[] columns = line.Split('\t');
            if (columns.Length < 10) continue;

            string recipeID = columns[0].Trim();
            string comp1 = columns[1].Trim();
            string comp2 = columns[2].Trim();
            string fusionName = columns[3].Trim();
            string tierStr = columns[4].Trim();
            string effect = columns[5].Trim();
            string requirements = columns[6].Trim();
            string baseDMGStr = columns[7].Trim().Replace("x", "");
            string atkSpeedStr = columns[8].Trim().Replace("/s", "");
            string rangeStr = columns[9].Trim();

            int tier = ParseTier(tierStr);

            // 1. Ensure tier folder exists
            string tierDirName = "Tier " + tierStr;
            string tierPath = fusionDir + "/" + tierDirName;
            if (!AssetDatabase.IsValidFolder(tierPath))
            {
                AssetDatabase.CreateFolder(fusionDir, tierDirName);
            }

            // 2. Find or Create Fusion Result EchoData
            string echoAssetPath = tierPath + "/" + fusionName.Replace(":", "").Replace("-", " ") + ".asset";
            EchoData resultEcho = AssetDatabase.LoadAssetAtPath<EchoData>(echoAssetPath);
            if (resultEcho == null)
            {
                resultEcho = ScriptableObject.CreateInstance<EchoData>();
                AssetDatabase.CreateAsset(resultEcho, echoAssetPath);
            }

            resultEcho.itemID = recipeID + "_RESULT";
            resultEcho.itemName = fusionName;
            resultEcho.description = effect;
            resultEcho.itemTier = tier;
            float.TryParse(baseDMGStr, out resultEcho.baseDamageMultiplier);
            float.TryParse(atkSpeedStr, out resultEcho.attacksPerSecond);
            
            if (System.Enum.TryParse(rangeStr, true, out EchoRange parsedRange))
                resultEcho.rangeCategory = parsedRange;

            resultEcho.echoType = EchoType.Composite;
            resultEcho.isFusionResult = true;
            resultEcho.uniqueModifierID = recipeID;
            EditorUtility.SetDirty(resultEcho);

            // 3. Find Component EchoDatas
            EchoData echoA = FindEchoData(comp1);
            EchoData echoB = FindEchoData(comp2);

            if (echoA == null) Debug.LogWarning($"Could not find EchoData for {comp1}");
            if (echoB == null) Debug.LogWarning($"Could not find EchoData for {comp2}");

            // 4. Find or Create FusionRecipeData
            string recipePath = recipesDir + "/" + recipeID + ".asset";
            FusionRecipeData recipe = AssetDatabase.LoadAssetAtPath<FusionRecipeData>(recipePath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<FusionRecipeData>();
                AssetDatabase.CreateAsset(recipe, recipePath);
            }

            recipe.recipeID = recipeID;
            recipe.echoA = echoA;
            recipe.echoB = echoB;
            recipe.resultEcho = resultEcho;
            recipe.recipeTier = tier;
            recipe.requiredConstellationNode = (requirements == "None") ? "" : requirements;

            EditorUtility.SetDirty(recipe);
            
            Debug.Log($"Imported: {fusionName} ({recipeID})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Fusion Recipe Import Complete!");
    }

    private static int ParseTier(string tierStr)
    {
        if (tierStr == "I") return 1;
        if (tierStr == "II") return 2;
        if (tierStr == "III") return 3;
        return 1;
    }

    private static EchoData FindEchoData(string name)
    {
        string[] guids = AssetDatabase.FindAssets("t:EchoData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EchoData data = AssetDatabase.LoadAssetAtPath<EchoData>(path);
            if (data != null && (data.itemName == name || data.uniqueModifierID == name || path.Contains(name)))
            {
                return data;
            }
        }
        return null;
    }
}