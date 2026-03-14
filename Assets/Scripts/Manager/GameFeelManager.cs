using UnityEngine;

public class GameFeelManager : MonoBehaviour
{
    public static GameFeelManager Instance { get; private set; }

    [Header("Camera Shake Intensities")]
    public float lightShakeIntensity = 1f;
    public float heavyShakeIntensity = 3f;

    [Header("Hit-Stop")]
    public float hitStopDuration = 0.1f;

    [Header("Damage Flash")]
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
        // 1. Visual flash on the enemy.
        SpriteColorFlasher flash = target.GetComponentInParent<SpriteColorFlasher>();
        if (flash != null)
        {
            SpriteRenderer sr = target.GetComponentInParent<SpriteRenderer>();
            if (sr != null)
                flash.FlashColor(sr, flashDuration, flashColor);
        }

        // 2. Global effects based on hit weight.
        if (isCrit || damage > 50)
        {
            CinemachineCameraShake2D.Instance?.ShakeCamera(heavyShakeIntensity);
            TimeManager.Instance?.DoHitStop(hitStopDuration);
        }
        else
        {
            CinemachineCameraShake2D.Instance?.ShakeCamera(lightShakeIntensity);
        }
    }

    public void ProcessPlayerHit()
    {
        CinemachineCameraShake2D.Instance?.ShakeCamera(heavyShakeIntensity);
        TimeManager.Instance?.DoHitStop(0.15f);
    }
}