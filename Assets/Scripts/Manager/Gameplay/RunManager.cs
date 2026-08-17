using UnityEngine;

public class RunManager : MonoBehaviour
{
    private static RunManager _instance;
    public static RunManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RunManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("RunManager");
                    _instance = go.AddComponent<RunManager>();
                }
            }
            return _instance;
        }
    }

    // Removed hardcoded diminishing returns curves; using Harmonic Decay algorithm via StatBonusSystem.

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.currentRun.currentLevelTime += Time.deltaTime;
        }
    }

    public void GrantBlessing(BlessingData blessing)
    {
        if (GameSession.Instance == null || GameSession.Instance.currentRun == null) return;
        RunData run = GameSession.Instance.currentRun;

        HealthSystem hs = PlayerStats.Instance != null
            ? PlayerStats.Instance.GetComponent<HealthSystem>()
            : null;

        // 1. Apply Vitality bonus through the centralized StatBonusSystem.
        //    This increments run.bonusVitality and applies Harmonic Decay HP gain.
        //    The shared bonusVitality counter means shrine and relic Vitality stacks
        //    compete on the same diminishing-return curve.
        if (blessing.grantedVitality > 0)
        {
            StatBonusSystem.ApplyVitalityBonus(run, hs, blessing.grantedVitality);
        }
        else if (blessing.path == BlessingPath.Vitality && blessing.grantedVitality == 0)
        {
            // Vitality path blessing with no explicit grantedVitality — grant one stack.
            StatBonusSystem.ApplyVitalityBonus(run, hs, 1);
        }

        // 2. Apply non-Vitality flat combat stat bonuses
        run.bonusSorcery   += blessing.grantedSorcery;
        run.bonusResonance += blessing.grantedResonance;

        // 3. Register special effect ID
        if (!string.IsNullOrEmpty(blessing.specialEffectID))
            run.activeBlessingEffects.Add(blessing.specialEffectID);
    }
}
