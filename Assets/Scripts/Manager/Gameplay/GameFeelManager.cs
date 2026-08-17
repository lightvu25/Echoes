using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum GameFeelImpactType
{
    NormalHit,
    CriticalHit,
    PlayerHit,
    Dash,
    KineticImpact,
    PlungeImpact,
    Explosion
}

[Serializable]
public sealed class GameFeelShakePreset
{
    [Min(0f)] public float intensity = 0.2f;
    [Min(0.01f)] public float duration = 0.12f;
    [Min(0f)] public float hitStopDuration;
    [Min(0f)] public float cooldown = 0.025f;
    public bool directional = true;
    public bool useDistanceFalloff;
    [Min(0.1f)] public float maxDistance = 18f;
    [Range(0f, 1f)] public float minimumFalloff = 0.15f;
}

[RequireComponent(typeof(CinemachineImpulseSource))]
public class GameFeelManager : MonoBehaviour
{
    public static GameFeelManager Instance { get; private set; }

    [Header("Legacy / Pickup Shake")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Combat Shake Presets")]
    [SerializeField] private GameFeelShakePreset normalHit = new GameFeelShakePreset
    {
        intensity = 0.12f,
        duration = 0.08f,
        hitStopDuration = 0.015f,
        cooldown = 0.02f
    };
    [SerializeField] private GameFeelShakePreset criticalHit = new GameFeelShakePreset
    {
        intensity = 0.45f,
        duration = 0.16f,
        hitStopDuration = 0.065f,
        cooldown = 0.04f
    };
    [SerializeField] private GameFeelShakePreset playerHit = new GameFeelShakePreset
    {
        intensity = 0.5f,
        duration = 0.18f,
        hitStopDuration = 0.06f,
        cooldown = 0.05f
    };
    [SerializeField] private GameFeelShakePreset dash = new GameFeelShakePreset
    {
        intensity = 0.09f,
        duration = 0.07f,
        hitStopDuration = 0f,
        cooldown = 0.05f,
        directional = true
    };
    [SerializeField] private GameFeelShakePreset kineticImpact = new GameFeelShakePreset
    {
        intensity = 0.28f,
        duration = 0.13f,
        hitStopDuration = 0.025f,
        cooldown = 0.035f
    };
    [SerializeField] private GameFeelShakePreset plungeImpact = new GameFeelShakePreset
    {
        intensity = 0.65f,
        duration = 0.22f,
        hitStopDuration = 0.05f,
        cooldown = 0.08f,
        directional = true
    };
    [SerializeField] private GameFeelShakePreset explosion = new GameFeelShakePreset
    {
        intensity = 0.8f,
        duration = 0.28f,
        hitStopDuration = 0.04f,
        cooldown = 0.06f,
        directional = false,
        useDistanceFalloff = true,
        maxDistance = 20f,
        minimumFalloff = 0.1f
    };

    private const string EnemyTag = "Enemy";

    private readonly Dictionary<GameFeelImpactType, CinemachineImpulseSource> presetSources =
        new Dictionary<GameFeelImpactType, CinemachineImpulseSource>();
    private readonly Dictionary<GameFeelImpactType, float> lastPlayTimes =
        new Dictionary<GameFeelImpactType, float>();

    public GameFeelImpactType? LastPlayedImpact { get; private set; }
    public event Action<GameFeelImpactType, float> OnImpactPlayed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
        BuildPresetSources();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildPresetSources()
    {
        presetSources.Clear();
        CreatePresetSource(GameFeelImpactType.NormalHit, normalHit);
        CreatePresetSource(GameFeelImpactType.CriticalHit, criticalHit);
        CreatePresetSource(GameFeelImpactType.PlayerHit, playerHit);
        CreatePresetSource(GameFeelImpactType.Dash, dash);
        CreatePresetSource(GameFeelImpactType.KineticImpact, kineticImpact);
        CreatePresetSource(GameFeelImpactType.PlungeImpact, plungeImpact);
        CreatePresetSource(GameFeelImpactType.Explosion, explosion);
    }

    private void CreatePresetSource(GameFeelImpactType type, GameFeelShakePreset preset)
    {
        CinemachineImpulseSource source = gameObject.AddComponent<CinemachineImpulseSource>();
        source.hideFlags = HideFlags.HideInInspector;
        source.ImpulseDefinition = new CinemachineImpulseDefinition
        {
            ImpulseChannel = impulseSource != null ? impulseSource.ImpulseDefinition.ImpulseChannel : 1,
            ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump,
            ImpulseDuration = Mathf.Max(0.01f, preset.duration),
            ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
            DissipationDistance = 100f,
            DissipationRate = 0.25f,
            PropagationSpeed = 343f
        };
        source.DefaultVelocity = Vector3.down;
        presetSources[type] = source;
    }

    public GameFeelShakePreset GetPreset(GameFeelImpactType type)
    {
        switch (type)
        {
            case GameFeelImpactType.NormalHit: return normalHit;
            case GameFeelImpactType.CriticalHit: return criticalHit;
            case GameFeelImpactType.PlayerHit: return playerHit;
            case GameFeelImpactType.Dash: return dash;
            case GameFeelImpactType.KineticImpact: return kineticImpact;
            case GameFeelImpactType.PlungeImpact: return plungeImpact;
            case GameFeelImpactType.Explosion: return explosion;
            default: return normalHit;
        }
    }

    public bool PlayImpact(
        GameFeelImpactType type,
        Vector3 worldPosition,
        Vector2 direction = default,
        float intensityScale = 1f)
    {
        GameFeelShakePreset preset = GetPreset(type);
        float now = Time.unscaledTime;
        if (lastPlayTimes.TryGetValue(type, out float lastTime) && now - lastTime < preset.cooldown)
            return false;

        float finalScale = Mathf.Max(0f, intensityScale) * CalculateDistanceFalloff(preset, worldPosition);
        if (finalScale <= 0f || preset.intensity <= 0f) return false;

        Vector2 impulseDirection = preset.directional
            ? direction.normalized
            : UnityEngine.Random.insideUnitCircle.normalized;
        if (impulseDirection.sqrMagnitude < 0.001f) impulseDirection = Vector2.down;

        if (!presetSources.TryGetValue(type, out CinemachineImpulseSource source) || source == null)
            return false;

        source.GenerateImpulseAtPositionWithVelocity(
            worldPosition,
            new Vector3(impulseDirection.x, impulseDirection.y, 0f) * preset.intensity * finalScale);

        lastPlayTimes[type] = now;
        LastPlayedImpact = type;
        OnImpactPlayed?.Invoke(type, finalScale);

        float scaledHitStop = preset.hitStopDuration * finalScale;
        if (scaledHitStop >= 0.005f)
            TimeManager.Instance?.DoHitStop(scaledHitStop);

        return true;
    }

    private static float CalculateDistanceFalloff(GameFeelShakePreset preset, Vector3 worldPosition)
    {
        if (!preset.useDistanceFalloff || Camera.main == null) return 1f;

        float distance = Vector2.Distance(Camera.main.transform.position, worldPosition);
        float normalized = 1f - Mathf.Clamp01(distance / Mathf.Max(0.1f, preset.maxDistance));
        return Mathf.Lerp(preset.minimumFalloff, 1f, normalized);
    }

    public void ProcessHit(
        GameObject attacker,
        GameObject victim,
        in DamageInfo damageInfo,
        int appliedDamage)
    {
        if (appliedDamage <= 0) return;

        // PlayerCombat owns player-hit feedback after it confirms that damage
        // was accepted. Skipping it here prevents one enemy attack from
        // producing both a generic hit shake and a player-hit shake.
        if (victim != null && victim.GetComponentInParent<PlayerCombat>() != null)
            return;

        bool attackerIsEnemy = attacker != null && attacker.CompareTag(EnemyTag);
        bool victimIsEnemy = victim != null && victim.CompareTag(EnemyTag);
        if (attackerIsEnemy && victimIsEnemy) return;

        bool isKinetic = damageInfo.activeEcho != null &&
                         damageInfo.activeEcho.uniqueModifierID == "KINETIC_FORCE";
        GameFeelImpactType type = damageInfo.isCritical
            ? GameFeelImpactType.CriticalHit
            : isKinetic
                ? GameFeelImpactType.KineticImpact
                : GameFeelImpactType.NormalHit;
        Vector3 position = victim != null ? victim.transform.position : transform.position;
        Vector2 direction = damageInfo.knockbackDirection;
        if (direction.sqrMagnitude < 0.001f && attacker != null && victim != null)
            direction = (victim.transform.position - attacker.transform.position).normalized;
        PlayImpact(type, position, direction);
    }

    // Backwards-compatible overload for older interactables.
    public void ProcessHit(GameObject attacker, GameObject victim, int damage, bool isCrit)
    {
        DamageInfo info = DamageInfo.Create(Mathf.Max(0, damage), attacker);
        info.isCritical = isCrit;
        ProcessHit(attacker, victim, info, Mathf.Max(0, damage));
    }

    public void ProcessPlayerHit(Vector3 position, Vector2 direction)
    {
        PlayImpact(GameFeelImpactType.PlayerHit, position, direction);
    }

    public void ProcessPlayerHit()
    {
        PlayImpact(GameFeelImpactType.PlayerHit, transform.position, Vector2.down);
    }

    public void ProcessDash(Vector3 position, Vector2 direction)
    {
        PlayImpact(GameFeelImpactType.Dash, position, -direction);
    }

    public void ProcessKineticImpact(Vector3 position, Vector2 direction)
    {
        PlayImpact(GameFeelImpactType.KineticImpact, position, direction);
    }

    public void ProcessPlungeImpact(Vector3 position, float dropDistance)
    {
        float scale = Mathf.Lerp(0.85f, 1.55f, Mathf.InverseLerp(2f, 12f, dropDistance));
        PlayImpact(GameFeelImpactType.PlungeImpact, position, Vector2.down, scale);
    }

    public void ProcessExplosion(Vector3 position, float intensityScale = 1f)
    {
        PlayImpact(GameFeelImpactType.Explosion, position, Vector2.down, intensityScale);
    }

    /// <summary>
    /// Legacy raw-force entry point retained for pickup and compatibility effects.
    /// New combat feedback should use a named impact preset instead.
    /// </summary>
    public void GenerateShake(float force)
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulseWithForce(force);
    }
}
