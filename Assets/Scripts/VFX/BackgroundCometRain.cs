using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns randomized comet prefabs just above the camera and moves them across
/// the background. Each controller owns its pool so comet lifetimes cannot
/// conflict with other pooled effects.
/// </summary>
public sealed class BackgroundCometRain : MonoBehaviour
{
    private sealed class PooledComet
    {
        public GameObject Prefab;
        public GameObject Instance;
        public Vector3 PrefabScale;
        public Vector3 Velocity;
        public float RemainingLifetime;
        public bool IsActive;
    }

    [Header("References")]
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private GameObject[] cometPrefabs;

    [Header("Spawning")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool spawnImmediately = true;
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(1.5f, 4f);
    [SerializeField] private Vector2 viewportXRange = new Vector2(-0.1f, 1.1f);
    [SerializeField] private Vector2 viewportYRange = new Vector2(1.05f, 1.2f);
    [SerializeField] private float worldZ = 5f;

    [Header("Movement")]
    [SerializeField] private Vector2 fallDirection = new Vector2(-0.35f, -1f);
    [SerializeField, Range(0f, 90f)] private float directionVariation = 15f;
    [SerializeField] private Vector2 speedRange = new Vector2(2.5f, 5f);
    [SerializeField] private Vector2 scaleRange = new Vector2(0.6f, 1.25f);
    [SerializeField] private Vector2 lifetimeRange = new Vector2(4f, 8f);
    [SerializeField] private bool alignRotationWithMovement = true;
    [SerializeField] private float spriteRotationOffset;

    private readonly List<PooledComet> cometPool = new List<PooledComet>();
    private Transform inactiveInstantiationRoot;
    private float spawnTimer;
    private bool isPlaying;

    private void Awake()
    {
        CreateInactiveInstantiationRoot();
    }

    private void OnEnable()
    {
        ResolveCamera();

        if (playOnEnable)
        {
            PlayRain();
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        UpdateActiveComets(deltaTime);

        if (!isPlaying || !HasValidPrefab()) return;

        spawnTimer -= deltaTime;
        if (spawnTimer > 0f) return;

        SpawnComet();
        ScheduleNextSpawn();
    }

    private void OnDisable()
    {
        isPlaying = false;
        ReturnAllComets();
    }

    public void PlayRain()
    {
        if (!isActiveAndEnabled || isPlaying) return;

        isPlaying = true;

        if (spawnImmediately)
        {
            SpawnComet();
        }

        ScheduleNextSpawn();
    }

    public void StopRain()
    {
        isPlaying = false;
    }

    public void SpawnComet()
    {
        if (!isActiveAndEnabled) return;

        ResolveCamera();
        GameObject prefab = GetRandomPrefab();
        if (spawnCamera == null || prefab == null) return;

        float viewportX = Random.Range(viewportXRange.x, viewportXRange.y);
        float viewportY = Random.Range(viewportYRange.x, viewportYRange.y);
        float cameraDepth = Mathf.Abs(worldZ - spawnCamera.transform.position.z);
        Vector3 spawnPosition = spawnCamera.ViewportToWorldPoint(
            new Vector3(viewportX, viewportY, cameraDepth));
        spawnPosition.z = worldZ;

        float randomAngle = Random.Range(-directionVariation, directionVariation);
        Vector2 baseDirection = fallDirection.sqrMagnitude > 0.001f
            ? fallDirection.normalized
            : Vector2.down;
        Vector2 direction = Quaternion.Euler(0f, 0f, randomAngle) * baseDirection;
        Vector3 velocity = direction * Random.Range(speedRange.x, speedRange.y);

        Quaternion rotation = Quaternion.identity;
        if (alignRotationWithMovement)
        {
            float movementAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rotation = Quaternion.Euler(0f, 0f, movementAngle + spriteRotationOffset);
        }

        PooledComet comet = GetOrCreateComet(prefab);
        if (comet == null || comet.Instance == null) return;

        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        comet.Instance.transform.SetPositionAndRotation(spawnPosition, rotation);
        comet.Instance.transform.localScale = comet.PrefabScale * randomScale;
        comet.Velocity = velocity;
        comet.RemainingLifetime = Random.Range(lifetimeRange.x, lifetimeRange.y);
        comet.IsActive = true;
        comet.Instance.SetActive(true);
    }

    private void UpdateActiveComets(float deltaTime)
    {
        for (int i = cometPool.Count - 1; i >= 0; i--)
        {
            PooledComet comet = cometPool[i];
            if (comet.Instance == null)
            {
                cometPool.RemoveAt(i);
                continue;
            }

            if (!comet.IsActive) continue;
            if (!comet.Instance.activeSelf)
            {
                comet.IsActive = false;
                continue;
            }

            comet.Instance.transform.position += comet.Velocity * deltaTime;
            comet.RemainingLifetime -= deltaTime;

            if (comet.RemainingLifetime <= 0f || IsOutsideCamera(comet.Instance.transform.position))
            {
                ReturnComet(comet);
            }
        }
    }

    private bool IsOutsideCamera(Vector3 worldPosition)
    {
        if (spawnCamera == null) return false;

        Vector3 viewportPosition = spawnCamera.WorldToViewportPoint(worldPosition);
        return viewportPosition.y < -0.25f
            || viewportPosition.x < -0.5f
            || viewportPosition.x > 1.5f;
    }

    private void ReturnAllComets()
    {
        for (int i = cometPool.Count - 1; i >= 0; i--)
        {
            if (cometPool[i].Instance != null)
            {
                ReturnComet(cometPool[i]);
            }
        }
    }

    private PooledComet GetOrCreateComet(GameObject prefab)
    {
        for (int i = 0; i < cometPool.Count; i++)
        {
            PooledComet pooledComet = cometPool[i];
            if (pooledComet.Prefab == prefab && !pooledComet.IsActive)
            {
                return pooledComet;
            }
        }

        CreateInactiveInstantiationRoot();
        GameObject instance = Instantiate(prefab, inactiveInstantiationRoot);
        instance.name = prefab.name + " (Background Pool)";

        // This controller is the sole lifetime owner for its private pool.
        ReturnToPool[] automaticReturns = instance.GetComponentsInChildren<ReturnToPool>(true);
        for (int i = 0; i < automaticReturns.Length; i++)
        {
            automaticReturns[i].enabled = false;
        }

        instance.SetActive(false);
        instance.transform.SetParent(transform, false);
        PooledComet newComet = new PooledComet
        {
            Prefab = prefab,
            Instance = instance,
            PrefabScale = prefab.transform.localScale
        };
        cometPool.Add(newComet);
        return newComet;
    }

    private void CreateInactiveInstantiationRoot()
    {
        if (inactiveInstantiationRoot != null) return;

        GameObject root = new GameObject("Inactive Comet Staging");
        inactiveInstantiationRoot = root.transform;
        inactiveInstantiationRoot.SetParent(transform, false);
        root.SetActive(false);
    }

    private static void ReturnComet(PooledComet comet)
    {
        comet.IsActive = false;
        comet.RemainingLifetime = 0f;
        if (comet.Instance != null)
        {
            comet.Instance.SetActive(false);
        }
    }

    private void ScheduleNextSpawn()
    {
        spawnTimer = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
    }

    private void ResolveCamera()
    {
        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }
    }

    private bool HasValidPrefab()
    {
        if (cometPrefabs == null) return false;

        for (int i = 0; i < cometPrefabs.Length; i++)
        {
            if (cometPrefabs[i] != null) return true;
        }

        return false;
    }

    private GameObject GetRandomPrefab()
    {
        if (!HasValidPrefab()) return null;

        int startIndex = Random.Range(0, cometPrefabs.Length);
        for (int i = 0; i < cometPrefabs.Length; i++)
        {
            GameObject prefab = cometPrefabs[(startIndex + i) % cometPrefabs.Length];
            if (prefab != null) return prefab;
        }

        return null;
    }

    private void OnValidate()
    {
        NormalizeRange(ref spawnIntervalRange, 0.01f);
        NormalizeRange(ref speedRange, 0f);
        NormalizeRange(ref scaleRange, 0.01f);
        NormalizeRange(ref lifetimeRange, 0.01f);
        NormalizeRange(ref viewportXRange, -2f);
        NormalizeRange(ref viewportYRange, -2f);
        directionVariation = Mathf.Clamp(directionVariation, 0f, 90f);
    }

    private static void NormalizeRange(ref Vector2 range, float minimumValue)
    {
        range.x = Mathf.Max(minimumValue, range.x);
        range.y = Mathf.Max(range.x, range.y);
    }
}
