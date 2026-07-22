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

    [Header("Testing")]
    [Tooltip("If true, blessings will always give the maximum value instead of diminishing over time.")]
    public bool consistentBlessings = true;

    // Diminishing Returns Curves
    // Each index = how many shrines of that path have already been taken
    private readonly int[] vitalityHPCurve = { 50, 50, 50, 30, 30, 30, 15, 15, 15, 15 };
    private readonly int[] damageHPCurve   = { 15, 15, 15,  5,  5,  5,  2,  2,  2,  2 };

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
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

        // 1. Calculate HP gain from path-specific diminishing returns curve
        int hpToGain = 0;
        if (blessing.path == BlessingPath.Vitality)
        {
            hpToGain = consistentBlessings ? vitalityHPCurve[0] : 
                (run.vitalityShrinesTaken < vitalityHPCurve.Length ? vitalityHPCurve[run.vitalityShrinesTaken] : 5);
            run.vitalityShrinesTaken++;
        }
        else if (blessing.path == BlessingPath.Sorcery)
        {
            hpToGain = consistentBlessings ? damageHPCurve[0] : 
                (run.sorceryShrinesTaken < damageHPCurve.Length ? damageHPCurve[run.sorceryShrinesTaken] : 2);
            run.sorceryShrinesTaken++;
        }
        else if (blessing.path == BlessingPath.Resonance)
        {
            hpToGain = consistentBlessings ? damageHPCurve[0] : 
                (run.resonanceShrinesTaken < damageHPCurve.Length ? damageHPCurve[run.resonanceShrinesTaken] : 2);
            run.resonanceShrinesTaken++;
        }

        // 2. Apply flat combat stat bonuses
        run.bonusVitality  += blessing.grantedVitality;
        run.bonusSorcery   += blessing.grantedSorcery;
        run.bonusResonance += blessing.grantedResonance;

        // 3. Register special effect ID
        if (!string.IsNullOrEmpty(blessing.specialEffectID))
            run.activeBlessingEffects.Add(blessing.specialEffectID);

        // 4. Apply HP gain to the live HealthSystem
        if (PlayerStats.Instance != null && hpToGain > 0)
        {
            HealthSystem hs = PlayerStats.Instance.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.SetMaxHP(hs.MaxHP + hpToGain, false);
                hs.Heal(hpToGain);
            }
        }
    }

}
