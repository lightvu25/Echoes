using UnityEngine;

[CreateAssetMenu(fileName = "New Playstyle", menuName = "Data/Playstyle Data")]
public class PlaystyleData : ScriptableObject
{
    [Header("Basic Info")]
    public PlaystyleType playstyleType;
    public string displayName;
    [Tooltip("If true, VFX will only spawn if an Echo is equipped.")]
    public bool requiresActiveEcho = false;
    
    [Header("Combo Settings")]
    public int comboSteps = 1;
    public float comboWindow = 0.4f;
    
    [Header("Per-Step Settings")]
    [Tooltip("Must match the length of comboSteps")]
    public float[] comboDamageMultipliers;
    [Tooltip("Must match the length of comboSteps")]
    public Vector2[] hitboxSizes;
    [Tooltip("Must match the length of comboSteps")]
    public Vector2[] hitboxOffsets;
    [Tooltip("Must match the length of comboSteps")]
    public string[] attackAnimationNames;
    [Tooltip("VFX Prefabs to spawn on each step of this playstyle's combo. Index 0 = Combo 1.")]
    public GameObject[] comboVFXPrefabs;
    [Tooltip("Must match the length of comboSteps. Offsets for VFX spawning.")]
    public Vector2[] vfxOffsets;
    
    [Header("Combat Stats")]
    public float procCoefficient = 1f;

    [Header("Long Range Specific")]
    public GameObject projectilePrefab;
    
    [Header("Magic Specific")]
    public float magicAoERadius = 3f;
    public GameObject magicAoEPrefab;
}
