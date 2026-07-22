using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewMindGardenNode", menuName = "Data/Mind Garden Node", order = 10)]
public class MindGardenNodeData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _skillID;
    [SerializeField] private string _skillName;
    
    [Range(1, 5)]
    [SerializeField] private int _tier = 1;

    [TextArea(2, 5)]
    [SerializeField] private string _description;

    [Header("Visuals")]
    [SerializeField] private Sprite _icon;
    
    [SerializeField] private Vector2 _uiPosition; 

    [Header("Requirements")]
    [Min(0)]
    [SerializeField] private int _memoryCost;

    [SerializeField] private List<MindGardenNodeData> _prerequisites = new List<MindGardenNodeData>();
    
    // --- PROPERTIES ---
    public string SkillID => _skillID;
    public string SkillName => _skillName;
    public int Tier => _tier;
    public string Description => _description;
    public Sprite Icon => _icon;
    public Vector2 UIPosition => _uiPosition;
    public int MemoryCost => _memoryCost;
    public IReadOnlyList<MindGardenNodeData> Prerequisites => _prerequisites;

    // --- LOGIC ---
    public bool ArePrerequisitesMet(ICollection<string> unlockedIDs)
    {
        if (_prerequisites == null || _prerequisites.Count == 0) return true;

        foreach (MindGardenNodeData prereq in _prerequisites)
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
            Debug.LogWarning($"[MindGardenNodeData] '{name}' has an empty SkillID. " +
                             "This MUST be set before the asset ships.", this);
    }
#endif
}
