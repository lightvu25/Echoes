using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class BaseLevelGenerator : MonoBehaviour
{
    [Header("Fallback")]
    [SerializeField] protected GameObject fallbackErrorRoom;

    public event Action<Transform> OnGenerationComplete;

    protected readonly List<GameObject> _spawnedRooms = new List<GameObject>();
    public IReadOnlyList<GameObject> SpawnedRooms => _spawnedRooms;

    public abstract void GenerateMap(int levelNumber);

    public abstract Transform GetPlayerSpawnPoint();

    protected void NotifyGenerationComplete()
    {
        OnGenerationComplete?.Invoke(GetPlayerSpawnPoint());
    }

    protected void ClearSpawnedRooms()
    {
        foreach (GameObject go in _spawnedRooms)
            if (go != null) Destroy(go);
        _spawnedRooms.Clear();
    }

    // Public teardown entry point for GameManager to call before regeneration
    public void ClearMap() => ClearSpawnedRooms();

    // Filters pool by required exit mask; returns fallbackErrorRoom on mismatch.
    protected GameObject GetMatchingRoomPrefab(List<GameObject> pool, RoomExitsMask requiredExits, bool exactMatch = false)
    {
        if (pool == null || pool.Count == 0)
        {
            Debug.LogError($"[LevelGen] Pool is null/empty. Required: {requiredExits}");
            return fallbackErrorRoom;
        }

        List<GameObject> valid = new List<GameObject>();
        foreach (GameObject prefab in pool)
        {
            if (prefab == null) continue;
            Room room = prefab.GetComponent<Room>();
            if (room == null) continue;

            room.CalculateExitsMask();
            RoomExitsMask mask = room.ExitsMask;

            bool match = exactMatch
                ? mask == requiredExits
                : (mask & requiredExits) == requiredExits;

            if (match) valid.Add(prefab);
        }

        if (valid.Count > 0)
            return valid[Random.Range(0, valid.Count)];

        Debug.LogError($"[LevelGen] No room matches required exits: {requiredExits}. Using fallback.");
        return fallbackErrorRoom;
    }
}
