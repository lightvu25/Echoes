#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EnemyBalanceValidator
{
    private const string EnemyDataFolder = "Assets/Data/Enemy";

    [MenuItem("Tools/Echoes/Validate Enemy Balance Data")]
    public static void ValidateEnemyBalanceData()
    {
        string[] guids = AssetDatabase.FindAssets("t:EnemyData", new[] { EnemyDataFolder });
        var problems = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                problems.Add($"Could not load EnemyData at {path}.");
                continue;
            }

            Validate(data, path, problems);
        }

        if (guids.Length == 0)
            problems.Add($"No EnemyData assets were found under {EnemyDataFolder}.");

        if (problems.Count == 0)
        {
            Debug.Log($"[EnemyBalanceValidator] Validated {guids.Length} EnemyData assets successfully.");
            EditorUtility.DisplayDialog("Enemy Balance", $"Validated {guids.Length} enemy data assets. No problems found.", "OK");
            return;
        }

        string report = string.Join("\n", problems);
        Debug.LogWarning($"[EnemyBalanceValidator] Found {problems.Count} problem(s):\n{report}");
        EditorUtility.DisplayDialog("Enemy Balance", $"Found {problems.Count} problem(s). See the Console for details.", "OK");
    }

    private static void Validate(EnemyData data, string path, List<string> problems)
    {
        Check(data.maxHP > 0, path, "Max HP must be greater than zero.", problems);
        Check(data.attackBase > 0, path, "Attack Base must be greater than zero.", problems);
        Check(data.attackRange > 0f, path, "Attack Range must be greater than zero.", problems);
        Check(data.visionRange >= data.attackRange, path, "Vision Range must cover Attack Range.", problems);
        Check(data.patrolWaitTimeMax >= data.patrolWaitTimeMin, path, "Patrol wait maximum is below its minimum.", problems);
        Check(data.expRewardMax >= data.expRewardMin, path, "EXP reward maximum is below its minimum.", problems);
        Check(data.goldAmountMax >= data.goldAmountMin, path, "Gold maximum is below its minimum.", problems);
        Check(data.astralShardAmountMax >= data.astralShardAmountMin, path, "Astral Shard maximum is below its minimum.", problems);
        Check(Approximately(data.patrolAccelAmount, 50f * data.patrolAcceleration / data.patrolMaxSpeed),
            path, "Patrol acceleration cache is stale; edit and save the asset to refresh it.", problems);
        Check(Approximately(data.patrolDeccelAmount, 50f * data.patrolDecceleration / data.patrolMaxSpeed),
            path, "Patrol deceleration cache is stale; edit and save the asset to refresh it.", problems);
        Check(Approximately(data.chaseAccelAmount, 50f * data.chaseAcceleration / data.chaseMaxSpeed),
            path, "Chase acceleration cache is stale; edit and save the asset to refresh it.", problems);
        Check(Approximately(data.chaseDeccelAmount, 50f * data.chaseDecceleration / data.chaseMaxSpeed),
            path, "Chase deceleration cache is stale; edit and save the asset to refresh it.", problems);
    }

    private static bool Approximately(float left, float right)
    {
        return Mathf.Abs(left - right) <= 0.01f;
    }

    private static void Check(bool condition, string path, string message, List<string> problems)
    {
        if (!condition)
            problems.Add($"{path}: {message}");
    }
}
#endif
