using UnityEngine;
using UnityEditor;
using System.IO;

public class EvolutionTierDataGenerator
{
    private const string TARGET_DIRECTORY = "Assets/Data/Evolution/";

    [MenuItem("Tools/Echoes/Generate Evolution Tiers")]
    public static void GenerateEvolutionTiers()
    {
        // 1. Ensure target directory exists
        EnsureDirectoryExists(TARGET_DIRECTORY);

        // 2. Generate the 4 assets
        CreateOrUpdateTier("Tier0_Base", 0, 1.0f, 1.0f, 1.0f, false, false, false);
        CreateOrUpdateTier("Tier1_Alert", 8, 0.8f, 1.0f, 1.2f, false, false, true);
        CreateOrUpdateTier("Tier2_Aggressive", 20, 0.6f, 0.8f, 1.5f, true, false, true);
        CreateOrUpdateTier("Tier3_Apex", 35, 0.5f, 0.7f, 999f, true, true, true);

        // 3. Save, refresh, and notify
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Successfully generated Evolution Tier assets at " + TARGET_DIRECTORY);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
        {
            string[] folders = path.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                if (string.IsNullOrEmpty(folders[i])) continue;

                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }
    }

    private static void CreateOrUpdateTier(string fileName, int reqKills, float telegraphMult, float cooldownMult, float visionMult, bool backstep, bool globalAggro, bool shareVision)
    {
        string assetPath = TARGET_DIRECTORY + fileName + ".asset";
        EvolutionTierData tierData = AssetDatabase.LoadAssetAtPath<EvolutionTierData>(assetPath);

        bool isNew = false;
        if (tierData == null)
        {
            tierData = ScriptableObject.CreateInstance<EvolutionTierData>();
            isNew = true;
        }

        tierData.tierName = fileName;
        tierData.requiredKills = reqKills;
        tierData.telegraphDurationMultiplier = telegraphMult;
        tierData.attackCooldownMultiplier = cooldownMult;
        tierData.visionRangeMultiplier = visionMult;
        tierData.canBackstep = backstep;
        tierData.isGlobalAggro = globalAggro;
        tierData.canShareVision = shareVision;

        if (isNew)
        {
            AssetDatabase.CreateAsset(tierData, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(tierData);
        }
    }
}
