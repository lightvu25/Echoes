#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class RelicDataGenerator
{
    private struct RelicRawData
    {
        public string RelicID;
        public string RelicNameEN;
        public RelicRarity Rarity;
        public string FactionBonus;
        public string CoreEffect;
    }

    private static readonly RelicRawData[] rawData = new RelicRawData[]
    {
        // Common (Tier 1)
        new RelicRawData { RelicID = "GraftingFlask", RelicNameEN = "Grafting Flask", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "Permanently grants +1 Max HP segment, but reduces movement speed by 5%." },
        new RelicRawData { RelicID = "BloodthirstyMoss", RelicNameEN = "Bloodthirsty Moss", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "Picking up currency/drops has a 5% chance to heal 1 HP." },
        new RelicRawData { RelicID = "AcidicGallbladder", RelicNameEN = "Acidic Gallbladder", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "Grants immunity to acid/poison pools and environmental hazards." },
        new RelicRawData { RelicID = "DarkBandage", RelicNameEN = "Dark Bandage", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "Extends Invincibility Frames (I-frames) by 0.5s after taking damage." },
        new RelicRawData { RelicID = "ShatteredMemory", RelicNameEN = "Shattered Memory", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "Killing an enemy grants +5% Attack Speed (Stacks up to 5 times). Resets per room." },
        new RelicRawData { RelicID = "RustyGrapple", RelicNameEN = "Rusty Grapple", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "Long-range hits hook distant enemies, pulling you toward them and stunning them for 0.5s." },
        new RelicRawData { RelicID = "RottenWeb", RelicNameEN = "Rotten Web", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "Holding the jump button slows descent (Float) and allows mid-air attacks without falling." },
        new RelicRawData { RelicID = "BurrowersScale", RelicNameEN = "Burrower's Scale", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "Reduces incoming physical Melee damage by 15%." },
        new RelicRawData { RelicID = "ToxicSpore", RelicNameEN = "Toxic Spore", Rarity = RelicRarity.Common, FactionBonus = "+1 Random Stat", CoreEffect = "The 3rd hit of your basic combo always inflicts Poison (Damage over time)." },

        // Rare (Tier 2)
        new RelicRawData { RelicID = "RustedCoin", RelicNameEN = "Rusted Coin", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "+1% Total Damage per 100 Currency/Mems held (Cap at +25%)." },
        new RelicRawData { RelicID = "EmberFirefly", RelicNameEN = "Ember Firefly", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Plunge impacts leave a burning zone on the ground for 3 seconds." },
        new RelicRawData { RelicID = "SpikedCleats", RelicNameEN = "Spiked Cleats", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Wall-jumping grants a 20% Movement Speed boost for 1.5 seconds." },
        new RelicRawData { RelicID = "EchoWhetstone", RelicNameEN = "Echo Whetstone", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Dashing through an enemy guarantees your next attack within 2s deals +100% damage." },
        new RelicRawData { RelicID = "BouncerShroom", RelicNameEN = "Bouncer Shroom", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Pressing jump right before landing on an enemy performs a Pogo-jump, dealing damage." },
        new RelicRawData { RelicID = "VialOfAshes", RelicNameEN = "Vial of Ashes", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Killing an enemy afflicted by an Element (Burn/Poison) triggers an AoE explosion." },
        new RelicRawData { RelicID = "BatsTalon", RelicNameEN = "Bat's Talon", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Aerial Attacks deal +30% Damage." },
        new RelicRawData { RelicID = "RustyHeavyChain", RelicNameEN = "Rusty Heavy Chain", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Plunge Attack shockwave radius and damage are increased by 50%." },
        new RelicRawData { RelicID = "DriedCyclopsEye", RelicNameEN = "Dried Cyclops Eye", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Entering a new room highlights enemies' weak points for 5 seconds (Guaranteed Crits)." },
        new RelicRawData { RelicID = "VolatileCore", RelicNameEN = "Volatile Core", Rarity = RelicRarity.Rare, FactionBonus = "+2 Random Stats", CoreEffect = "Negates knockback from explosions and reflects 50% of the blast damage outwards." },

        // Legendary (Tier 3)
        new RelicRawData { RelicID = "BloodContract", RelicNameEN = "Blood Contract", Rarity = RelicRarity.Legendary, FactionBonus = "+3 Stats (All)", CoreEffect = "Shop items cost 0 Currency, but permanently cost 1 Max HP segment to purchase." },
        new RelicRawData { RelicID = "OreSparkCore", RelicNameEN = "Ore Spark Core", Rarity = RelicRarity.Legendary, FactionBonus = "+3 Stats (All)", CoreEffect = "Every 3rd basic attack unleashes Chain Lightning that strikes 3 nearby enemies." },
        new RelicRawData { RelicID = "StalactiteHeart", RelicNameEN = "Stalactite Heart", Rarity = RelicRarity.Legendary, FactionBonus = "+3 Stats (All)", CoreEffect = "Upon taking fatal damage: Destroys this Relic, heals 50% HP, and freezes enemies for 3s." },
        new RelicRawData { RelicID = "SoulBell", RelicNameEN = "Soul Bell", Rarity = RelicRarity.Legendary, FactionBonus = "+3 Stats (All)", CoreEffect = "Defeating an Elite/Boss grants a Shield that negates 1 hit without dropping items." },
        new RelicRawData { RelicID = "CondemnedRing", RelicNameEN = "Condemned Ring", Rarity = RelicRarity.Legendary, FactionBonus = "+3 Stats (All)", CoreEffect = "Max HP is permanently locked to 1. All attacks are guaranteed Critical Hits." },
        new RelicRawData { RelicID = "EchoingSigil", RelicNameEN = "Echoing Sigil", Rarity = RelicRarity.Legendary, FactionBonus = "+3 Stats (All)", CoreEffect = "Every attack creates a delayed Echo that hits again after 0.25s for 60% damage." },
        new RelicRawData { RelicID = "AbyssalTreads", RelicNameEN = "Abyssal Treads", Rarity = RelicRarity.Legendary, FactionBonus = "+3 Stats (All)", CoreEffect = "Unlocks Triple Jump. The third jump releases a kinetic blast pushing enemies away." },
        new RelicRawData { RelicID = "VampiricFang", RelicNameEN = "Vampiric Fang", Rarity = RelicRarity.Legendary, FactionBonus = "+3 Stats (All)", CoreEffect = "Killing an enemy with a Plunge Attack heals 10% of Max HP." }
    };

    [MenuItem("Tools/Echoes/Generate Relic Data")]
    public static void GenerateRelicData()
    {
        string folderPath = "Assets/Data/Relics";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        int createdCount = 0;
        int updatedCount = 0;

        foreach (var data in rawData)
        {
            string assetPath = $"{folderPath}/{data.RelicID}.asset";
            RelicData existingAsset = AssetDatabase.LoadAssetAtPath<RelicData>(assetPath);

            if (existingAsset != null)
            {
                // Update existing
                PopulateRelicData(existingAsset, data);
                EditorUtility.SetDirty(existingAsset);
                updatedCount++;
            }
            else
            {
                // Create new
                RelicData newAsset = ScriptableObject.CreateInstance<RelicData>();
                PopulateRelicData(newAsset, data);
                AssetDatabase.CreateAsset(newAsset, assetPath);
                createdCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Relic Data Generation Complete! Created: {createdCount}, Updated: {updatedCount}. Total Relics: {rawData.Length}");
    }

    private static void PopulateRelicData(RelicData relic, RelicRawData data)
    {
        relic.Rarity = data.Rarity;
        relic.FactionBonus = data.FactionBonus;

        
        // Map to ItemBaseData fields to ensure compatibility with inventory/shop
        relic.itemID = data.RelicID;
        relic.itemName = data.RelicNameEN;
        relic.description = data.CoreEffect;
        relic.itemTier = (int)data.Rarity + 1; // Common=1, Rare=2, Legendary=3
        relic.basePrice = 50; // default base price
    }
}
#endif
