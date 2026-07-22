using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;
    
    [Range(0f, 100f)]
    public float spawnPercentage = 33.3f; 
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.7f;

    [Header("Enemy Pool")]
    [SerializeField] private List<EnemySpawnEntry> possibleEnemies = new();

    public void Init(int currentTier)
    {
        // Fallback for older prefabs that serialized spawnChance to 0
        if (spawnChance <= 0f) spawnChance = 0.7f;

        float finalSpawnChance = Mathf.Clamp01(spawnChance + ((currentTier - 1) * 0.05f));

        if (Random.value > finalSpawnChance)
        {
            Destroy(gameObject);
            return;
        }

        if (possibleEnemies == null || possibleEnemies.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawner] No enemy prefabs assigned at {transform.position}.", this);
            Destroy(gameObject);
            return;
        }

        SpawnEnemy();
        Destroy(gameObject);
    }

    private void SpawnEnemy()
    {
        float totalWeight = 0f;
        foreach (var entry in possibleEnemies)
        {
            totalWeight += entry.spawnPercentage;
        }

        if (totalWeight <= 0f) return;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        GameObject selectedPrefab = null;

        foreach (var entry in possibleEnemies)
        {
            currentWeight += entry.spawnPercentage;
            if (randomValue <= currentWeight)
            {
                selectedPrefab = entry.enemyPrefab;
                break;
            }
        }

        if (selectedPrefab == null) return;

        GameObject container = GameObject.Find("EnemiesContainer")
            ?? new GameObject("EnemiesContainer") { transform = { position = Vector3.zero } };

        GameObject enemy = Instantiate(selectedPrefab, transform.position, Quaternion.identity, container.transform);
        enemy.name = selectedPrefab.name;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.35f);
        Gizmos.DrawSphere(transform.position, 0.4f);

        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}