using UnityEngine;

/// <summary>
/// Dedicated Singleton to manage Mind Node branches and path states.
/// Handles the Risk/Reward mechanics of cutting or connecting nodes in the Mind Garden.
/// </summary>
public class MindPathManager : MonoBehaviour
{
    private static MindPathManager _instance;
    public static MindPathManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MindPathManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MindPathManager");
                    _instance = go.AddComponent<MindPathManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    /// <summary>
    /// Accepts the selected path and injects the node's risk/reward modifiers directly into the run data.
    /// </summary>
    public void AcceptNodePath(MindNode node)
    {
        if (node == null || GameSession.Instance == null || GameSession.Instance.currentRun == null) 
            return;

        MindNodeModifierData data = node.ModifierData;
        if (data == null)
        {
            Debug.Log($"[MindPathManager] Accepted safe node {node.name} with no modifiers.");
            return;
        }

        RunData run = GameSession.Instance.currentRun;

        if (node.nodeType == NodeType.Relic)
        {
            run.minGuaranteedRelics += Random.Range(1, 3);
        }
        else if (node.nodeType == NodeType.Echo)
        {
            run.minGuaranteedEchoes += Random.Range(1, 3);
        }
        else if (node.nodeType == NodeType.Equipment)
        {
            run.minGuaranteedEquipment += Random.Range(1, 3);
        }

        // Apply Rewards
        run.bonusRelicChance += data.bonusRelicChance;
        run.bonusEquipmentChance += data.bonusEquipmentChance;
        run.bonusEchoChance += data.bonusEchoChance;

        // Apply Risks
        run.magicToxicity += data.magicToxicityIncrease;
        run.enemyDensityMultiplier *= data.enemyDensityMultiplier;

        if (data.addedEliteEnemyTypes != null)
        {
            foreach (var elite in data.addedEliteEnemyTypes)
            {
                if (!run.addedEliteEnemyTypes.Contains(elite))
                {
                    run.addedEliteEnemyTypes.Add(elite);
                }
            }
        }

        Debug.Log($"[MindPathManager] Accepted node {node.name}. Modifiers applied. Current Toxicity: {run.magicToxicity}");
    }
}
