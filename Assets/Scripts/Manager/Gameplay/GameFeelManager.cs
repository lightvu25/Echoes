using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class GameFeelManager : MonoBehaviour
{
    public static GameFeelManager Instance { get; private set; }

    [SerializeField] private float lightShakeForce  = 0.5f;
    [SerializeField] private float heavyShakeForce  = 1.5f;
    [SerializeField] private float playerHitShakeForce = 2f;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Hit-Stop")]
    [SerializeField] private float lightHitStopDuration = 0f;
    [SerializeField] private float heavyHitStopDuration = 0.1f;

    private const string TAG_ENEMY  = "Enemy";
    private const string TAG_PLAYER = "Player";

    private void Awake()
    {
        Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();    
        
        if (impulseSource == null)
        {
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            Debug.LogWarning("GameFeelManager: CinemachineImpulseSource bị thiếu và đã được tự động thêm. Vui lòng gán Noise Profile cho nó trong Editor.");
        }        
    }

    public void ProcessHit(GameObject attacker, GameObject victim, int damage, bool isCrit)
    {
        bool attackerIsEnemy = attacker != null && attacker.CompareTag(TAG_ENEMY);
        bool victimIsEnemy   = victim   != null && victim.CompareTag(TAG_ENEMY);

        bool suppressShake = attackerIsEnemy && victimIsEnemy;

        if (!suppressShake)
        {
            if (isCrit || damage > 50)
                GenerateShake(heavyShakeForce);
            else
                GenerateShake(lightShakeForce);
        }

        if (isCrit || damage > 50)
            TimeManager.Instance?.DoHitStop(heavyHitStopDuration);
        else if (lightHitStopDuration > 0f)
            TimeManager.Instance?.DoHitStop(lightHitStopDuration);
    }

    public void ProcessPlayerHit()
    {
        GenerateShake(playerHitShakeForce);
        TimeManager.Instance?.DoHitStop(heavyHitStopDuration);
    }

    public void GenerateShake(float force)
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulse(force);
    }
}