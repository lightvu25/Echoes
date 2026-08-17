using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(HealthSystem))]
public class EchoEffectManager : MonoBehaviour
{
    [Header("Fusion Prefabs")]
    [SerializeField] private GameObject eventHorizonPrefab;
    [SerializeField] private GameObject fireTrailPrefab;
    [SerializeField] private GameObject glitchedZonePrefab;
    [SerializeField] private GameObject echoExplosionPrefab;

    private AttackHitbox playerAttackHitbox;
    private HealthSystem playerHealth;
    private FusionRuntimeHost fusionRuntimeHost;

    private EchoModifierContext context;
    private List<IEchoModifier> activeModifiers = new List<IEchoModifier>();
    private Dictionary<string, Func<IEchoModifier>> modifierFactory = new Dictionary<string, Func<IEchoModifier>>();

    private void Awake()
    {
        playerAttackHitbox = GetComponentInChildren<AttackHitbox>(true);
        playerHealth = GetComponent<HealthSystem>();
        fusionRuntimeHost = GetComponent<FusionRuntimeHost>();
        if (fusionRuntimeHost == null) fusionRuntimeHost = gameObject.AddComponent<FusionRuntimeHost>();

        context = new EchoModifierContext
        {
            PlayerAttackHitbox = playerAttackHitbox,
            PlayerHealth = playerHealth,
            PlayerGameObject = gameObject,
            EventHorizonPrefab = eventHorizonPrefab,
            FireTrailPrefab = fireTrailPrefab,
            GlitchedZonePrefab = glitchedZonePrefab,
            EchoExplosionPrefab = echoExplosionPrefab
        };

        // Register modifiers
        modifierFactory["IGNITION"] = () => new BlazeModifier();
        modifierFactory["FROSTBITE"] = () => new FrostbiteModifier();
        modifierFactory["CHAIN_ARC"] = () => new ArcModifier();
        modifierFactory["DISTORTION"] = () => new AnomalyModifier();
        modifierFactory["OBLIVION"] = () => new CurseModifier();
        modifierFactory["KINETIC_FORCE"] = () => new KineticModifier();
        modifierFactory["VOID_MARK"] = () => new VoidModifier();

        modifierFactory["FUS_CRYO_STASIS"] = () => new CryoStasisModifier();
        modifierFactory["FUS_ENTROPY"] = () => new EntropyModifier();
        modifierFactory["FUS_EVENT_HORIZON"] = () => new EventHorizonModifier();
        modifierFactory["FUS_NEON_GRID"] = () => new NeonGridModifier();
        modifierFactory["FUS_AFTERBURNER"] = () => new AfterburnerModifier();
        modifierFactory["FUS_RAGNAROK"] = () => new RagnarokModifier();
        modifierFactory["FUS_PLASMA"] = () => new PlasmaModifier();
        modifierFactory["FUS_AVALANCHE"] = () => new AvalancheModifier();
        modifierFactory["FUS_OVERCLOCK"] = () => new OverclockModifier();
        modifierFactory["FUS_SUPERNOVA"] = () => new SupernovaModifier();
        modifierFactory["FUS_DEATH_DRIVE"] = () => new DeathDriveModifier();
        modifierFactory["FUS_ZERO_POINT"] = () => new ZeroPointModifier();
    }

    private void OnEnable()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void Start()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged -= HandleInventoryChanged; // Ensure no double subscribe
            PlayerInventoryCore.Instance.OnInventoryChanged += HandleInventoryChanged;
            
            // Initial apply
            HandleInventoryChanged();
        }
    }

    private void OnDisable()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
        ClearModifiers();
    }

    private void HandleInventoryChanged()
    {
        ClearModifiers();

        if (PlayerInventoryCore.Instance == null) return;

        EchoData activeEcho = PlayerInventoryCore.Instance.GetActiveEcho();
        if (activeEcho == null) return;

        ApplyEcho(activeEcho);
    }

    public void ApplyEcho(EchoData echo)
    {
        if (echo == null) return;

        context.ActiveEchoData = echo;

        if (modifierFactory.TryGetValue(echo.uniqueModifierID, out Func<IEchoModifier> constructor))
        {
            IEchoModifier modifier = constructor();
            modifier.Initialize(context);
            activeModifiers.Add(modifier);
        }

        activeModifiers = activeModifiers.OrderByDescending(m => m.Priority).ToList();
    }

    public void ApplyFusion(EchoData[] echoes)
    {
        foreach (var echo in echoes)
        {
            if (echo != null)
            {
                if (modifierFactory.TryGetValue(echo.uniqueModifierID, out Func<IEchoModifier> constructor))
                {
                    IEchoModifier modifier = constructor();
                    modifier.Initialize(context);
                    activeModifiers.Add(modifier);
                }
            }
        }
        activeModifiers = activeModifiers.OrderByDescending(m => m.Priority).ToList();
    }

    private void ClearModifiers()
    {
        foreach (var modifier in activeModifiers)
        {
            modifier.Remove();
        }
        activeModifiers.Clear();
    }

    public void HandlePlayerDash(Vector3 startPos, Vector3 endPos)
    {
        foreach (var modifier in activeModifiers)
        {
            if (modifier is IEchoDashModifier dashMod)
            {
                dashMod.OnDash(startPos, endPos);
            }
        }
    }
}
