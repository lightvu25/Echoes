#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ToolDataGenerator
{
    private struct ToolEntry
    {
        public string idName;
        public string itemName;
        public string durability;
        public int usage;
        public float cooldown;
        public string coreEffect;
    }

    private static readonly ToolEntry[] ToolsData = new ToolEntry[]
    {
        new ToolEntry { idName = "itm_blood_bomb", itemName = "Blood-Ore Bomb", durability = "NULL", usage = 1, cooldown = 3.0f, coreEffect = "Throws a bomb that explodes after 2s for AoE damage." },
        new ToolEntry { idName = "itm_crimson_dart", itemName = "Crimson Dart", durability = "NULL", usage = 10, cooldown = 0.5f, coreEffect = "Fires a high-speed piercing projectile." },
        new ToolEntry { idName = "itm_adrenaline_vial", itemName = "Adrenaline Vial", durability = "NULL", usage = 1, cooldown = 10.0f, coreEffect = "+50% move/attack speed for 5s." },
        new ToolEntry { idName = "itm_arachne_trap", itemName = "Arachne Trap", durability = "NULL", usage = 1, cooldown = 5.0f, coreEffect = "Places a trap that roots enemies for 3s upon contact." },
        new ToolEntry { idName = "itm_toxic_flask", itemName = "Toxic Flask", durability = "NULL", usage = 1, cooldown = 8.0f, coreEffect = "Smashes to create a poison cloud dealing continuous damage." },
        new ToolEntry { idName = "itm_void_aegis", itemName = "Void Aegis", durability = "NULL", usage = 1, cooldown = 15.0f, coreEffect = "Grants complete damage immunity for 3s." },
        new ToolEntry { idName = "itm_kinetic_root", itemName = "Kinetic Root", durability = "NULL", usage = 1, cooldown = 4.0f, coreEffect = "Emits a non-damaging shockwave that knocks back enemies." }
    };

    [MenuItem("Tools/Echoes/Generate Tactical Tools Data")]
    public static void GenerateTools()
    {
        string directoryPath = "Assets/Data/Tools";

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
        }

        int created = 0;
        int updated = 0;

        foreach (var entry in ToolsData)
        {
            string assetPath = $"{directoryPath}/{entry.idName}.asset";
            ToolData existingTool = AssetDatabase.LoadAssetAtPath<ToolData>(assetPath);

            int parsedDurability = 0;
            if (entry.durability != "NULL")
            {
                int.TryParse(entry.durability, out parsedDurability);
            }

            string formattedID = entry.idName.Replace("itm_", "").ToUpper();

            if (existingTool != null)
            {
                existingTool.itemID = formattedID;
                existingTool.itemName = entry.itemName;
                existingTool.durability = parsedDurability;
                existingTool.maxUses = entry.usage;
                existingTool.cooldown = entry.cooldown;
                existingTool.description = entry.coreEffect;
                
                EditorUtility.SetDirty(existingTool);
                updated++;
            }
            else
            {
                ToolData newTool = ScriptableObject.CreateInstance<ToolData>();
                newTool.itemID = formattedID;
                newTool.itemName = entry.itemName;
                newTool.durability = parsedDurability;
                newTool.maxUses = entry.usage;
                newTool.cooldown = entry.cooldown;
                newTool.description = entry.coreEffect;

                AssetDatabase.CreateAsset(newTool, assetPath);
                created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ToolDataGenerator] Generation complete. Created: {created}, Updated: {updated} ToolData assets.");
    }
}
#endif
