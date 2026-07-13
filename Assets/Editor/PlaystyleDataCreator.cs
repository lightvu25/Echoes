using UnityEngine;
using UnityEditor;
using System.IO;

public class PlaystyleDataCreator
{
    [MenuItem("Tools/Echoes/Create Playstyle Data Profiles")]
    public static void CreatePlaystyleProfiles()
    {
        string folderPath = "Assets/Data/Combat";
        
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Combat");
        }

        // 1. Melee
        CreatePlaystyleData(folderPath, "MeleePlaystyle", PlaystyleType.Melee, 3, "Melee_Combo_");
        
        // 2. MidRange
        CreatePlaystyleData(folderPath, "MidRangePlaystyle", PlaystyleType.MidRange, 2, "MidRange_Combo_");
        
        // 3. LongRange
        CreatePlaystyleData(folderPath, "LongRangePlaystyle", PlaystyleType.LongRange, 1, "LongRange_Combo_");
        
        // 4. Magic
        CreatePlaystyleData(folderPath, "MagicPlaystyle", PlaystyleType.Magic, 1, "Magic_Burst_");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Object folder = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);
        
        Debug.Log("Successfully created 4 Playstyle Data profiles in " + folderPath);
    }

    private static void CreatePlaystyleData(string path, string fileName, PlaystyleType type, int comboSteps, string animPrefix)
    {
        string assetPath = $"{path}/{fileName}.asset";
        
        // Check if already exists
        PlaystyleData existing = AssetDatabase.LoadAssetAtPath<PlaystyleData>(assetPath);
        if (existing != null)
        {
            Debug.LogWarning($"Asset already exists at {assetPath}. Skipping creation.");
            return;
        }

        PlaystyleData newProfile = ScriptableObject.CreateInstance<PlaystyleData>();
        newProfile.playstyleType = type;
        newProfile.displayName = type.ToString();
        newProfile.comboSteps = comboSteps;
        newProfile.comboWindow = 0.4f;
        newProfile.procCoefficient = 1f;
        
        newProfile.comboDamageMultipliers = new float[comboSteps];
        newProfile.hitboxSizes = new Vector2[comboSteps];
        newProfile.hitboxOffsets = new Vector2[comboSteps];
        newProfile.attackAnimationNames = new string[comboSteps];

        for (int i = 0; i < comboSteps; i++)
        {
            newProfile.comboDamageMultipliers[i] = 1.0f + (i * 0.2f);
            newProfile.hitboxSizes[i] = new Vector2(1f, 1f);
            newProfile.hitboxOffsets[i] = new Vector2(0.5f, 0f);
            
            if (type == PlaystyleType.Magic)
            {
                newProfile.attackAnimationNames[i] = "Magic_Burst";
            }
            else
            {
                newProfile.attackAnimationNames[i] = $"{animPrefix}{i + 1}"; // e.g. "Melee_Combo_1"
            }
        }

        if (type == PlaystyleType.Magic)
        {
            newProfile.requiresActiveEcho = true;
            newProfile.magicAoERadius = 3f;
        }

        AssetDatabase.CreateAsset(newProfile, assetPath);
    }
}
