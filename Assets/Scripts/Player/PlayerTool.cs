using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthSystem), typeof(PlayerBuffManager))]
public class PlayerTool : MonoBehaviour
{
    [Header("Tool Prefabs")]
    [SerializeField] private GameObject bloodOreBombPrefab;
    [SerializeField] private GameObject crimsonDartPrefab;
    [SerializeField] private GameObject arachneTrapPrefab;
    [SerializeField] private GameObject toxicFlaskPrefab;
    [SerializeField] private GameObject toxicCloudPrefab;
    [SerializeField] private GameObject kineticRootPrefab;

    private float[] cooldowns = new float[3];
    private PlayerBuffManager buffManager;

    public event System.Action OnConsume;

    private void Awake()
    {
        buffManager = GetComponent<PlayerBuffManager>();
    }

    private void Start()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnToolKeyPressed += HandleToolInput;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnToolKeyPressed -= HandleToolInput;
        }
    }

    private void Update()
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0)
            {
                cooldowns[i] -= Time.deltaTime;
            }
        }
    }

    private void HandleToolInput(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= cooldowns.Length) return;
        if (cooldowns[slotIndex] > 0) return;
        if (PlayerInventoryCore.Instance == null) return;

        IReadOnlyList<ItemBaseData> equippedTools = PlayerInventoryCore.Instance.EquippedTools;
        if (slotIndex >= equippedTools.Count) return;

        ToolData activeTool = equippedTools[slotIndex] as ToolData;
        if (activeTool == null) return;

        ExecuteToolLogic(activeTool);
        cooldowns[slotIndex] = activeTool.cooldown;
    }

    private void ExecuteToolLogic(ToolData tool)
    {
        Vector3 spawnPos = transform.position + (transform.right * (transform.localScale.x > 0 ? 0.5f : -0.5f));
        float faceDir = Mathf.Sign(transform.localScale.x);

        switch (tool.itemName)
        {
            case "Blood-Ore Bomb":
                if (bloodOreBombPrefab != null)
                {
                    GameObject bomb = ObjectPoolManager.SpawnObject(bloodOreBombPrefab, spawnPos, Quaternion.identity, ObjectPoolManager.PoolType.Projectile);
                    var bombRb = bomb.GetComponent<Rigidbody2D>();
                    if (bombRb != null) bombRb.AddForce(new Vector2(faceDir * 5f, 5f), ForceMode2D.Impulse);
                    bomb.GetComponent<BloodOreBomb>()?.Initialize(gameObject);
                }
                break;

            case "Crimson Dart":
                if (crimsonDartPrefab != null)
                {
                    GameObject dart = ObjectPoolManager.SpawnObject(crimsonDartPrefab, spawnPos, Quaternion.identity, ObjectPoolManager.PoolType.Projectile);
                    var dartRb = dart.GetComponent<Rigidbody2D>();
                    if (dartRb != null) dartRb.linearVelocity = new Vector2(faceDir * 15f, 0f);
                    dart.GetComponent<CrimsonDart>()?.Initialize(gameObject);
                }
                break;

            case "Adrenaline Vial":
                if (buffManager != null) buffManager.ActivateAdrenaline(5f, 1.5f);
                OnConsume?.Invoke();
                break;

            case "Arachne Trap":
                if (arachneTrapPrefab != null)
                {
                    ObjectPoolManager.SpawnObject(arachneTrapPrefab, transform.position - new Vector3(0, 0.5f, 0), Quaternion.identity, ObjectPoolManager.PoolType.Projectile);
                }
                break;

            case "Toxic Flask":
                if (toxicFlaskPrefab != null)
                {
                    GameObject flask = ObjectPoolManager.SpawnObject(toxicFlaskPrefab, spawnPos, Quaternion.identity, ObjectPoolManager.PoolType.Projectile);
                    var flaskRb = flask.GetComponent<Rigidbody2D>();
                    if (flaskRb != null) flaskRb.AddForce(new Vector2(faceDir * 4f, 2f), ForceMode2D.Impulse);
                    flask.GetComponent<ToxicFlask>()?.Initialize(gameObject, toxicCloudPrefab);
                }
                break;

            case "Void Aegis":
                if (buffManager != null) buffManager.ActivateVoidAegis(3f);
                OnConsume?.Invoke();
                break;

            case "Kinetic Root":
                if (kineticRootPrefab != null)
                {
                    GameObject root = ObjectPoolManager.SpawnObject(kineticRootPrefab, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.Projectile);
                    root.GetComponent<KineticRoot>()?.Initialize(gameObject);
                }
                break;
        }
    }
}
