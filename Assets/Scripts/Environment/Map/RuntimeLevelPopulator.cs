using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RuntimeLevelPopulator : MonoBehaviour
{
    public static RuntimeLevelPopulator Instance { get; private set; }

    [Header("Event Prefabs")]
    public GameObject teleporterPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private static int _teleporterCount = 1;

    // Accept the level's blueprint so spawn config is per-level
    public void PopulateRooms(List<Room> rooms, LevelBlueprint blueprint)
    {
        _teleporterCount = 1;

        foreach (Room room in rooms)
        {
            if (room == null) continue;

            EntityAnchor[] anchors = room.GetComponentsInChildren<EntityAnchor>();

            foreach (EntityAnchor anchor in anchors)
            {
                ProcessEventAnchor(anchor, room.AssignedEvent, blueprint);
            }
        }
        
        Debug.Log("[RuntimeLevelPopulator] Population complete.");
    }

    private bool ProcessEventAnchor(EntityAnchor anchor, RoomEventType roomEvent, LevelBlueprint blueprint)
    {
        if (anchor.anchorType == AnchorType.Teleporter && roomEvent == RoomEventType.Teleport)
        {
            if (teleporterPrefab != null)
            {
                GameObject tpObj = Instantiate(teleporterPrefab, anchor.transform.position, Quaternion.identity, anchor.transform.parent);
                TeleporterNode tpNode = tpObj.GetComponent<TeleporterNode>();
                if (tpNode != null)
                {
                    tpNode.nodeName = "Teleporter " + _teleporterCount;
                    _teleporterCount++;
                }
                Destroy(anchor.gameObject);
                return true;
            }
        }
        else if (anchor.anchorType == AnchorType.Echo_Common)
        {
            // If the room itself was specifically flagged with the 'Echo' dynamic event, guarantee a spawn!
            bool forceSpawn = (roomEvent == RoomEventType.Echo);

            // Independent roll for this anchor (if not forced)
            if (!forceSpawn && Random.value > blueprint.anchorSpawnChance)
            {
                Destroy(anchor.gameObject);
                return false;
            }

            // Anchor survived the roll — pick a node from the weighted pool
            GameObject chosenPrefab = PickWeightedEchoNode(blueprint.echoNodePool);
            if (chosenPrefab != null)
            {
                Instantiate(chosenPrefab, anchor.transform.position, Quaternion.identity, anchor.transform.parent);
            }
            Destroy(anchor.gameObject);
            return chosenPrefab != null;
        }
        else if (anchor.anchorType == AnchorType.Enemy_Ground || anchor.anchorType == AnchorType.Enemy_Air)
        {
            float baseChance = (anchor.anchorType == AnchorType.Enemy_Ground) ? blueprint.groundEnemySpawnChance : blueprint.airEnemySpawnChance;
            
            // 1a. Tier Scaling (Old EnemySpawner logic restored)
            int currentTier = GameSession.Instance?.currentRun?.currentLevel ?? 1;
            float scaledChance = Mathf.Clamp01(baseChance + ((currentTier - 1) * 0.05f));

            // 1b. Difficulty Scaling (Density from Mind World modifiers)
            float densityMult = GameSession.Instance?.currentRun?.enemyDensityMultiplier ?? 1.0f;
            
            if (Random.value > (scaledChance * densityMult))
            {
                Destroy(anchor.gameObject);
                return false;
            }

            // 2. Pick base enemy
            var pool = (anchor.anchorType == AnchorType.Enemy_Ground) ? blueprint.groundEnemyPool : blueprint.airEnemyPool;
            GameObject chosenPrefab = PickWeightedEnemyNode(pool);
            
            // 3. Toxicity Upgrade: Check if this should become an Elite
            float toxicity = GameSession.Instance?.currentRun?.magicToxicity ?? 0f;
            if (Random.Range(0f, 100f) < toxicity)
            {
                // Attempt to spawn an Elite from RunData's added types instead
                var run = GameSession.Instance?.currentRun;
                if (run != null && run.addedEliteEnemyTypes != null && run.addedEliteEnemyTypes.Count > 0)
                {
                    // Pick a random Elite name from the pool
                    string eliteName = run.addedEliteEnemyTypes[Random.Range(0, run.addedEliteEnemyTypes.Count)];
                    
                    // Fetch the elite prefab from the LevelBlueprint's registered list
                    if (blueprint.availableElitePrefabs != null)
                    {
                        GameObject elitePrefab = null;
                        foreach (GameObject prefab in blueprint.availableElitePrefabs)
                        {
                            if (prefab != null && prefab.name == eliteName)
                            {
                                elitePrefab = prefab;
                                break;
                            }
                        }

                        if (elitePrefab != null)
                        {
                            chosenPrefab = elitePrefab;
                            Debug.Log($"[RuntimeLevelPopulator] Toxicity triggered! Spawned Elite: {eliteName}");
                        }
                    }
                }
            }

            if (chosenPrefab != null)
            {
                // 4. Hierarchy Organization: Per-Room "EnemiesContainer"
                Transform roomTransform = anchor.transform.parent;
                Transform enemiesContainer = roomTransform.Find("EnemiesContainer");
                
                if (enemiesContainer == null)
                {
                    GameObject containerObj = new GameObject("EnemiesContainer");
                    containerObj.transform.SetParent(roomTransform);
                    containerObj.transform.localPosition = Vector3.zero;
                    enemiesContainer = containerObj.transform;
                }

                GameObject enemy = Instantiate(chosenPrefab, anchor.transform.position, Quaternion.identity, enemiesContainer);
                enemy.name = chosenPrefab.name; // Keep name clean like old spawner did
            }
            Destroy(anchor.gameObject);
            return chosenPrefab != null;
        }
        return false;
    }

    private GameObject PickWeightedEchoNode(List<EchoNodeRate> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        float totalWeight = pool.Sum(e => e.weight);
        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (EchoNodeRate entry in pool)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return entry.nodePrefab;
        }

        return pool[pool.Count - 1].nodePrefab;
    }

    private GameObject PickWeightedEnemyNode(List<EnemyNodeRate> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        float totalWeight = pool.Sum(e => e.weight);
        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (EnemyNodeRate entry in pool)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return entry.enemyPrefab;
        }

        return pool[pool.Count - 1].enemyPrefab;
    }
}
