using UnityEngine;

public class GameFeelManager : MonoBehaviour
{
    public static GameFeelManager Instance;

    [Header("Settings")]
    public float lightShakeIntensity = 1f;
    public float heavyShakeIntensity = 3f;
    public float hitStopDuration = 0.1f;
    public float flashDuration = 0.1f;
    public Color flashColor = Color.white;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Call this whenever the player hits an enemy
    public void ProcessAttack(GameObject target, int damage, bool isCrit)
    {
        // 1. Handle Visuals on the Enemy (Flash)
        // We look for the flash script LOCALLY on the enemy we just hit
        if (target.TryGetComponent(out SpriteColorFlasher flash))
        {
            SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
            flash.FlashColor(targetRenderer, flashDuration, flashColor);
        }

        // 2. Handle Global Effects based on Impact
        if (isCrit || damage > 50)
        {
            // HEAVY IMPACT
            if (CameraShaker.Instance != null)
                CameraShaker.Instance.BasicShake(heavyShakeIntensity, 0.2f);

            if (TimeManager.Instance != null)
                TimeManager.Instance.DoHitStop(hitStopDuration);
        }
        else
        {
            // LIGHT IMPACT
            if (CameraShaker.Instance != null)
                CameraShaker.Instance.BasicShake(lightShakeIntensity, 0.1f);
        }
    }

    // Call this when the PLAYER takes damage
    public void ProcessPlayerHit()
    {
        if (CameraShaker.Instance != null)
            CameraShaker.Instance.BasicShake(heavyShakeIntensity, 0.3f);

        if (TimeManager.Instance != null)
            TimeManager.Instance.DoHitStop(0.15f);
    }
}