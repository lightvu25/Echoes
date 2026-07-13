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
            // Independent roll for this anchor
            if (Random.value > blueprint.anchorSpawnChance)
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
}
