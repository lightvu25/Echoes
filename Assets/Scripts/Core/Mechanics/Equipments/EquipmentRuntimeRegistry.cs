using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the runtime construction details for Equipment. PlayerTool only handles
/// input, inventory lookup, and cooldowns; this registry maps stable item IDs to
/// independently testable execution strategies.
/// </summary>
[DisallowMultipleComponent]
public sealed class EquipmentRuntimeRegistry : MonoBehaviour
{
    private static readonly HashSet<string> SupportedToolIds = new HashSet<string>(StringComparer.Ordinal)
    {
        "BOMB", "CRIMSON_DART", "ADRENALINE_VIAL", "ARACHNE_TRAP", "TOXIC_FLASK", "VOID_AEGIS", "KINETIC_ROOT"
    };
    [Header("Equipment Prefabs")]
    [SerializeField] private GameObject bloodOreBombPrefab;
    [SerializeField] private GameObject crimsonDartPrefab;
    [SerializeField] private GameObject arachneTrapPrefab;
    [SerializeField] private GameObject toxicFlaskPrefab;
    [SerializeField] private GameObject toxicCloudPrefab;
    [SerializeField] private GameObject kineticRootPrefab;

    private readonly Dictionary<string, Func<EquipmentUseContext, bool>> executors =
        new Dictionary<string, Func<EquipmentUseContext, bool>>(StringComparer.Ordinal);

    private void Awake()
    {
        BuildRegistry();
    }

    public bool TryExecute(ToolData tool, EquipmentUseContext context)
    {
        if (tool == null || string.IsNullOrWhiteSpace(tool.itemID)) return false;
        if (executors.Count == 0) BuildRegistry();

        if (!executors.TryGetValue(tool.itemID, out Func<EquipmentUseContext, bool> executor))
        {
            Debug.LogWarning($"[EquipmentRuntimeRegistry] No runtime behavior registered for '{tool.itemID}'.", tool);
            return false;
        }

        return executor(context);
    }

    public static bool SupportsTool(string itemId) => !string.IsNullOrEmpty(itemId) && SupportedToolIds.Contains(itemId);

    public bool HasExecutor(string itemId)
    {
        if (executors.Count == 0) BuildRegistry();
        return !string.IsNullOrEmpty(itemId) && executors.ContainsKey(itemId);
    }

    private void BuildRegistry()
    {
        executors.Clear();
        executors["BOMB"] = ThrowBomb;
        executors["CRIMSON_DART"] = FireCrimsonDart;
        executors["ADRENALINE_VIAL"] = UseAdrenaline;
        executors["ARACHNE_TRAP"] = PlaceArachneTrap;
        executors["TOXIC_FLASK"] = ThrowToxicFlask;
        executors["VOID_AEGIS"] = UseVoidAegis;
        executors["KINETIC_ROOT"] = UseKineticRoot;
    }

    private bool ThrowBomb(EquipmentUseContext context)
    {
        if (bloodOreBombPrefab == null) return false;

        GameObject bomb = Spawn(bloodOreBombPrefab, context.ForwardSpawnPosition);
        BloodOreBomb effect = bomb.GetComponent<BloodOreBomb>();
        if (effect == null) return FailMisconfiguredSpawn(bomb, bloodOreBombPrefab, nameof(BloodOreBomb));
        Rigidbody2D body = bomb.GetComponent<Rigidbody2D>();
        ResetBody(body);
        body?.AddForce(new Vector2(context.FacingDirection * 5f, 5f), ForceMode2D.Impulse);
        effect.Initialize(context.Owner);
        return true;
    }

    private bool FireCrimsonDart(EquipmentUseContext context)
    {
        if (crimsonDartPrefab == null) return false;

        GameObject dart = Spawn(crimsonDartPrefab, context.ForwardSpawnPosition);
        CrimsonDart effect = dart.GetComponent<CrimsonDart>();
        if (effect == null) return FailMisconfiguredSpawn(dart, crimsonDartPrefab, nameof(CrimsonDart));
        Rigidbody2D body = dart.GetComponent<Rigidbody2D>();
        ResetBody(body);
        if (body != null) body.linearVelocity = new Vector2(context.FacingDirection * 15f, 0f);
        effect.Initialize(context.Owner);
        return true;
    }

    private bool UseAdrenaline(EquipmentUseContext context)
    {
        if (context.BuffManager == null) return false;
        context.BuffManager.ActivateAdrenaline(5f, 1.5f);
        context.NotifyConsumed();
        return true;
    }

    private bool PlaceArachneTrap(EquipmentUseContext context)
    {
        if (arachneTrapPrefab == null) return false;
        GameObject trap = Spawn(arachneTrapPrefab, context.Owner.transform.position - new Vector3(0f, 0.5f, 0f));
        if (trap.GetComponent<ArachneTrap>() == null) return FailMisconfiguredSpawn(trap, arachneTrapPrefab, nameof(ArachneTrap));
        return true;
    }

    private bool ThrowToxicFlask(EquipmentUseContext context)
    {
        if (toxicFlaskPrefab == null || toxicCloudPrefab == null) return false;
        if (toxicCloudPrefab.GetComponent<ToxicCloud>() == null)
        {
            Debug.LogError($"[EquipmentRuntimeRegistry] '{toxicCloudPrefab.name}' is missing required component {nameof(ToxicCloud)}.", toxicCloudPrefab);
            return false;
        }

        GameObject flask = Spawn(toxicFlaskPrefab, context.ForwardSpawnPosition);
        ToxicFlask effect = flask.GetComponent<ToxicFlask>();
        if (effect == null) return FailMisconfiguredSpawn(flask, toxicFlaskPrefab, nameof(ToxicFlask));
        Rigidbody2D body = flask.GetComponent<Rigidbody2D>();
        ResetBody(body);
        body?.AddForce(new Vector2(context.FacingDirection * 4f, 2f), ForceMode2D.Impulse);
        effect.Initialize(context.Owner, toxicCloudPrefab);
        return true;
    }

    private bool UseVoidAegis(EquipmentUseContext context)
    {
        if (context.BuffManager == null) return false;
        context.BuffManager.ActivateVoidAegis(3f);
        context.NotifyConsumed();
        return true;
    }

    private bool UseKineticRoot(EquipmentUseContext context)
    {
        if (kineticRootPrefab == null) return false;
        GameObject root = Spawn(kineticRootPrefab, context.Owner.transform.position);
        KineticRoot effect = root.GetComponent<KineticRoot>();
        if (effect == null) return FailMisconfiguredSpawn(root, kineticRootPrefab, nameof(KineticRoot));
        effect.Initialize(context.Owner);
        return true;
    }

    private static GameObject Spawn(GameObject prefab, Vector3 position)
    {
        return ObjectPoolManager.SpawnObject(
            prefab,
            position,
            Quaternion.identity,
            ObjectPoolManager.PoolType.Projectile);
    }

    private static void ResetBody(Rigidbody2D body)
    {
        if (body == null) return;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private static bool FailMisconfiguredSpawn(GameObject spawned, GameObject prefab, string requiredComponent)
    {
        Debug.LogError($"[EquipmentRuntimeRegistry] '{prefab.name}' is missing required component {requiredComponent}.", prefab);
        ObjectPoolManager.ReturnObjectToPool(spawned);
        return false;
    }
}

public readonly struct EquipmentUseContext
{
    private readonly Action consumed;

    public EquipmentUseContext(GameObject owner, PlayerBuffManager buffManager, Action consumed)
    {
        Owner = owner;
        BuffManager = buffManager;
        this.consumed = consumed;
    }

    public GameObject Owner { get; }
    public PlayerBuffManager BuffManager { get; }
    public float FacingDirection => Mathf.Sign(Owner.transform.localScale.x);
    public Vector3 ForwardSpawnPosition =>
        Owner.transform.position + Owner.transform.right * (FacingDirection > 0f ? 0.5f : -0.5f);

    public void NotifyConsumed() => consumed?.Invoke();
}
