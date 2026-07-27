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



    // Removed hardcoded diminishing returns curves; using Harmonic Decay algorithm.

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

        // 1. Calculate HP gain from path-specific diminishing returns curve
        int hpToGain = 0;
        if (blessing.path == BlessingPath.Vitality)
        {
            hpToGain = Mathf.Max(BlessingCalculator.CalculateStackBonus(50, run.vitalityShrinesTaken + 1), 5);
            run.vitalityShrinesTaken++;
        }
        else if (blessing.path == BlessingPath.Sorcery)
        {
            hpToGain = Mathf.Max(BlessingCalculator.CalculateStackBonus(15, run.sorceryShrinesTaken + 1), 2);
            run.sorceryShrinesTaken++;
        }
        else if (blessing.path == BlessingPath.Resonance)
        {
            hpToGain = Mathf.Max(BlessingCalculator.CalculateStackBonus(15, run.resonanceShrinesTaken + 1), 2);
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
