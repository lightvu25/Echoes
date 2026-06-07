using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines one node in the Statue meta-progression skill tree.
/// Create instances via Assets ▶ Create ▶ Project Echoes ▶ Statue Skill.
/// </summary>
[CreateAssetMenu(
    fileName = "NewStatueSkill",
    menuName  = "Project Echoes/Statue Skill",
    order     = 10)]
public class StatueSkillData : ScriptableObject
{
    // ------------------------------------------------------------------ //
    //  Identity                                                            //
    // ------------------------------------------------------------------ //

    [Tooltip("Unique string key stored in ProfileData.unlockedSkillIDs. " +
             "Never change this after shipping — it is the save-file key.")]
    [SerializeField] private string _skillID;

    [Tooltip("Display name shown in the info panel.")]
    [SerializeField] private string _skillName;

    [Tooltip("Flavour / mechanical description shown in the info panel.")]
    [TextArea(2, 5)]
    [SerializeField] private string _description;

    // ------------------------------------------------------------------ //
    //  Visuals                                                             //
    // ------------------------------------------------------------------ //

    [Tooltip("Icon rendered on the skill node button.")]
    [SerializeField] private Sprite _icon;

    // ------------------------------------------------------------------ //
    //  Cost                                                                //
    // ------------------------------------------------------------------ //

    [Tooltip("Banked Memory Fragments required to unlock this skill.")]
    [Min(0)]
    [SerializeField] private int _memoryCost;

    // ------------------------------------------------------------------ //
    //  Dependencies                                                        //
    // ------------------------------------------------------------------ //

    [Tooltip("All listed skills must be unlocked before this one is purchasable.")]
    [SerializeField] private List<StatueSkillData> _prerequisites = new List<StatueSkillData>();

    // ------------------------------------------------------------------ //
    //  Public read-only API                                               //
    // ------------------------------------------------------------------ //

    /// <summary>Unique save-file key. Must never be changed after shipping.</summary>
    public string SkillID => _skillID;

    /// <summary>Human-readable display name.</summary>
    public string SkillName => _skillName;

    /// <summary>Flavour / mechanical description.</summary>
    public string Description => _description;

    /// <summary>Icon shown on the node button.</summary>
    public Sprite Icon => _icon;

    /// <summary>Banked Memory Fragment cost to purchase.</summary>
    public int MemoryCost => _memoryCost;

    /// <summary>Skills that must already be unlocked before this one is available.</summary>
    public IReadOnlyList<StatueSkillData> Prerequisites => _prerequisites;

    // ------------------------------------------------------------------ //
    //  Helpers                                                             //
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Returns true if every prerequisite skill is present in the supplied unlocked-set.
    /// </summary>
    /// <param name="unlockedIDs">The player's current list of unlocked skill IDs.</param>
    public bool ArePrerequisitesMet(ICollection<string> unlockedIDs)
    {
        if (_prerequisites == null || _prerequisites.Count == 0) return true;

        foreach (StatueSkillData prereq in _prerequisites)
        {
            if (prereq == null) continue;
            if (!unlockedIDs.Contains(prereq.SkillID)) return false;
        }
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_skillID))
            Debug.LogWarning($"[StatueSkillData] '{name}' has an empty SkillID. " +
                             "This MUST be set before the asset ships.", this);
    }
#endif
}
