using System;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(EnemyCombat))]
public class LootSpawner : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnemyData enemyData;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject goldPrefab;
    [SerializeField] private GameObject astralShardPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float popForceMin = 5f;
    [SerializeField] private float popForceMax = 10f;
    [SerializeField] private float sidewaysForceMod = 1.5f;
    
    [SerializeField] private int minBurstObjects = 3;
    [SerializeField] private int maxBurstObjects = 7;

    private EnemyCombat enemyCombat;

    private void Awake()
    {
        enemyCombat = GetComponent<EnemyCombat>();
    }

    private void Start()
    {
        if (enemyCombat != null)
        {
            enemyCombat.OnEnemyDied += HandleEnemyDeath;
        }
    }

    private void OnDestroy()
    {
        if (enemyCombat != null)
        {
            enemyCombat.OnEnemyDied -= HandleEnemyDeath;
        }
    }

    private void HandleEnemyDeath(object sender, EventArgs e)
    {
        if (enemyData == null) return;

        GrantExp();
        SpawnLoot();
    }

    private void GrantExp()
    {
        // Instantly grant EXP to player
        int expToReward = Random.Range(enemyData.expRewardMin, enemyData.expRewardMax + 1);
        
        if (expToReward > 0)
        {
            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddExp(expToReward);
            }
        }
    }

    private void SpawnLoot()
    {
        // --- Astral Shards ---
        if (Random.Range(0f, 100f) <= enemyData.astralShardDropChance)
        {
            int shardAmount = Random.Range(enemyData.astralShardAmountMin, enemyData.astralShardAmountMax + 1);
            if (shardAmount > 0 && astralShardPrefab != null)
            {
                SpawnCollectible(astralShardPrefab, shardAmount);
            }
        }

        // --- Tính Gold ---
        if (Random.Range(0f, 100f) <= enemyData.goldDropChance)
        {
            int remainingGold = Random.Range(enemyData.goldAmountMin, enemyData.goldAmountMax + 1);
            
            if (remainingGold > 0 && goldPrefab != null)
            {
                // Force coinsToSpawn to be at least 5, or clamp bounded by remainingGold logic
                int targetSpawns = Mathf.Max(5, Mathf.Min(remainingGold, maxBurstObjects));

                int[] chunks = new int[targetSpawns];
                
                // Divvy up the gold randomly to ensure value variety
                for (int i = 0; i < remainingGold; i++)
                {
                    chunks[Random.Range(0, targetSpawns)]++;
                }

                foreach (int chunk in chunks)
                {
                    SpawnCollectible(goldPrefab, chunk);
                }
            }
        }
    }

    private void SpawnCollectible(GameObject prefab, int amount)
    {
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        GameObject collectibleObj = ObjectPoolManager.SpawnObject(prefab, spawnPos, Quaternion.identity, ObjectPoolManager.PoolType.Loot);

        if (collectibleObj.TryGetComponent(out ResourceDrop resourceDrop))
        {
            float dirX = 1f;
            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                dirX = Mathf.Sign(playerStats.transform.position.x - transform.position.x);
            }

            if (dirX == 0) dirX = Random.value > 0.5f ? 1f : -1f;

            float randomX = dirX * Random.Range(3f, 5f) * sidewaysForceMod; 
            float randomY = Random.Range(popForceMin, popForceMax); 

            Vector2 popForce = new Vector2(randomX, randomY);

            resourceDrop.Initialize(amount, popForce);
        }
    }
}
