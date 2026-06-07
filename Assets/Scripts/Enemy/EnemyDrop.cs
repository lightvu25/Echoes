using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Attach to any enemy to give it a LootTable-driven item drop on death.
/// Hooks into EnemyCombat.OnEnemyDied – no modifications to existing classes required.
/// </summary>
[RequireComponent(typeof(EnemyCombat))]
public class EnemyDrop : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EnemyData enemyData;

    [Header("Loot")]
    [SerializeField] private LootTable lootTable;

    [Tooltip("1f = normal, 1.5f = elite, 2f = boss, etc.")]
    [SerializeField] private float dropChanceMultiplier = 1f;

    [Tooltip("Minimum item tier allowed to drop (non-consumables).")]
    [Range(1, 3)] [SerializeField] private int minTierAllowed = 1;

    [Header("Physics Burst")]
    [SerializeField] private float popForceMin = 4f;
    [SerializeField] private float popForceMax = 9f;
    [SerializeField] private float sidewaysForceMod = 1.5f;
    [SerializeField] private int maxBurstObjects = 7;

    private EnemyCombat enemyCombat;

    private void Awake() => enemyCombat = GetComponent<EnemyCombat>();

    private void Start()
    {
        if (enemyCombat != null)
            enemyCombat.OnEnemyDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (enemyCombat != null)
            enemyCombat.OnEnemyDied -= HandleDeath;
    }

    private void HandleDeath(object sender, EventArgs e)
    {
        GrantExp();
        DropLoot();
    }

    private void GrantExp()
    {
        if (enemyData == null) return;

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

    public void DropLoot()
    {
        if (lootTable == null) return;

        // --- Forgotten_Hourglass Relic ---
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.TryGetComponent<InventoryManager>(out var inv) && inv.HasRelic("Forgotten_Hourglass"))
        {
            return;
        }

        List<DropResult> drops = lootTable.GetDrops(dropChanceMultiplier, minTierAllowed);
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        float dirX = 1f;
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            dirX = Mathf.Sign(playerStats.transform.position.x - transform.position.x);
        }
        if (dirX == 0) dirX = Random.value > 0.5f ? 1f : -1f;

        foreach (DropResult result in drops)
        {
            if (result.type == LootItemType.Currency)
            {
                int remaining = result.totalAmount;
                int targetSpawns = Mathf.Max(3, Mathf.Min(remaining, maxBurstObjects));
                int[] chunks = new int[targetSpawns];

                for (int i = 0; i < remaining; i++)
                {
                    chunks[Random.Range(0, targetSpawns)]++;
                }

                foreach (int chunk in chunks)
                {
                    if (chunk <= 0) continue;
                    GameObject obj = ObjectPoolManager.SpawnObject(result.prefab, origin, Quaternion.identity, ObjectPoolManager.PoolType.Loot);
                    if (obj.TryGetComponent(out Collectible collectible))
                    {
                        float randomX = dirX * Random.Range(3f, 5f) * sidewaysForceMod; 
                        float randomY = Random.Range(popForceMin, popForceMax); 
                        collectible.Initialize(chunk, new Vector2(randomX, randomY));
                    }
                }
            }
            else
            {
                for (int i = 0; i < result.totalAmount; i++)
                {
                    GameObject obj = Instantiate(result.prefab, origin, Quaternion.identity);
                    if (obj.TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        float randomX = Random.Range(-1f, 1f) * sidewaysForceMod;
                        float randomY = Random.Range(popForceMin, popForceMax);
                        rb.AddForce(new Vector2(randomX, randomY), ForceMode2D.Impulse);
                    }
                }
            }
        }
    }
}
