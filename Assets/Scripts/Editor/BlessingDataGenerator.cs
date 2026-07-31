using UnityEditor;
using UnityEngine;

public static class BlessingDataGenerator
{
    private const string OUTPUT_FOLDER = "Assets/Data/Blessings";

    private struct BlessingDef
    {
        public BlessingPath path;
        public string buffName;
        public string specialEffectID;
        public string description;
        public int grantedVitality;
        public int grantedSorcery;
        public int grantedResonance;

        public BlessingDef(BlessingPath path, string buffName, string effectID, string desc,
                           int vit, int sor, int res)
        {
            this.path          = path;
            this.buffName      = buffName;
            specialEffectID    = effectID;
            description        = desc;
            grantedVitality    = vit;
            grantedSorcery     = sor;
            grantedResonance   = res;
        }
    }

    private static readonly BlessingDef[] Blessings = new BlessingDef[]
    {
        // Vitality
        new BlessingDef(BlessingPath.Vitality,  "Lumin Bloodline",     "LUMIN_BLOODLINE",     "15% chance to drop a 10 HP healing orb upon defeating an enemy.",                       1, 2, 2),
        new BlessingDef(BlessingPath.Vitality,  "Starlight Carapace",  "STARLIGHT_CARAPACE",  "Gain a temporary shield equal to 10% of Max HP when entering a new combat room.",       1, 2, 2),
        new BlessingDef(BlessingPath.Vitality,  "Martyr's Grace",      "MARTYRS_GRACE",       "Increases all healing received by 15%.",                                               1, 2, 2),
        new BlessingDef(BlessingPath.Vitality,  "Void Resonance",      "VOID_RESONANCE",      "Losing 1 slot HP will reduce 10% damage received for 3 seconds.",                     1, 2, 2),

        // Sorcery
        new BlessingDef(BlessingPath.Sorcery,   "Blade of Memories",   "BLADE_MEMORIES",      "Deals 20% bonus damage to enemies below 30% HP.",                                     2, 1, 2),
        new BlessingDef(BlessingPath.Sorcery,   "Shattered Star",      "SHATTERED_STAR",      "Critical hit damage is increased by 20%.",                                            2, 1, 2),
        new BlessingDef(BlessingPath.Sorcery,   "Hidden Tempest",      "HIDDEN_TEMPEST",      "The first attack immediately following a Dash gains 15% increased attack speed.",      2, 1, 2),
        new BlessingDef(BlessingPath.Sorcery,   "Zephyr's Sting",      "ZEPHYR_STING",        "Moving continuously for 3 seconds buffs your next attack's damage by 20%.",           2, 1, 2),

        // Resonance
        new BlessingDef(BlessingPath.Resonance, "Echo of Ruin",        "ECHO_RUIN",           "Basic attacks have a 10% chance to trigger an Echo AoE explosion with 50% its damage.", 2, 2, 1),
        new BlessingDef(BlessingPath.Resonance, "Gravity Well",        "GRAVITY_WELL",        "The final hit of your basic attack combo slightly pulls nearby enemies inward.",        2, 2, 1),
        new BlessingDef(BlessingPath.Resonance, "Random Nova",         "RANDOM_NOVA",         "Defeating an enemy has a 15% chance to trigger a random Echo explosion.",              2, 2, 1),
        new BlessingDef(BlessingPath.Resonance, "Relic Luxury",        "RELIC_LUXURY",        "Increases 10% chance of mobs dropping relics.",                                       2, 2, 1),
    };

    [MenuItem("Tools/Echoes/Generate Traces of Regret")]
    private static void Generate()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");

        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
            AssetDatabase.CreateFolder("Assets/Data", "Blessings");

        foreach (BlessingDef def in Blessings)
        {
            string safeName = def.buffName.Replace("'", "").Replace(" ", "_");
            string assetPath = $"{OUTPUT_FOLDER}/{safeName}.asset";

            BlessingData asset = AssetDatabase.LoadAssetAtPath<BlessingData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BlessingData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("buffName").stringValue         = def.buffName;
            so.FindProperty("description").stringValue      = def.description;
            so.FindProperty("path").enumValueIndex          = (int)def.path;
            so.FindProperty("specialEffectID").stringValue  = def.specialEffectID;
            so.FindProperty("grantedVitality").intValue     = def.grantedVitality;
            so.FindProperty("grantedSorcery").intValue      = def.grantedSorcery;
            so.FindProperty("grantedResonance").intValue    = def.grantedResonance;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BlessingDataGenerator] Generated {Blessings.Length} Blessing assets in '{OUTPUT_FOLDER}'.");
    }
}
