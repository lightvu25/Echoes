using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WaterZone : MonoBehaviour
{
    [Header("Physics Modifiers")]
    [SerializeField] private float playerSpeedMultiplier = 0.6f;
    [SerializeField] private float waterLinearDrag = 3f;

    [Header("Electrification VFX")]
    [SerializeField] private Color electrifiedTint = new Color(0.5f, 0.8f, 1f, 0.8f);
    [SerializeField] private float electrifiedDuration = 0.5f;

    private readonly HashSet<IDamageable> _entitiesInWater = new HashSet<IDamageable>();
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer != null)
            _originalColor = _spriteRenderer.color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
            _entitiesInWater.Add(damageable);

        PlayerMovement playerMove = other.GetComponent<PlayerMovement>();
        if (playerMove != null)
        {
            playerMove.SetWaterSpeedMultiplier(playerSpeedMultiplier);
            playerMove.SetLinearDrag(waterLinearDrag);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
            _entitiesInWater.Remove(damageable);

        PlayerMovement playerMove = other.GetComponent<PlayerMovement>();
        if (playerMove != null)
        {
            playerMove.SetWaterSpeedMultiplier(1f);
            playerMove.ResetLinearDrag();
        }
    }

    /// <summary>
    /// Shocks all damageable entities currently in the water zone.
    /// Call this from any electricity source (e.g., a lightning trap or electric projectile).
    /// </summary>
    public void Electrify(GameObject attacker, int damage, float stunDuration)
    {
        _entitiesInWater.RemoveWhere(e => e == null || e.IsDead);

        DamageInfo shockInfo = DamageInfo.Create(damage, attacker);
        shockInfo.damageSource = "ElectrifiedWater";
        shockInfo.knockbackForce = 0f;
        shockInfo.hitFreezeTime = stunDuration;

        foreach (IDamageable target in _entitiesInWater)
        {
            if (target == null || target.IsDead) continue;
            if (target.Transform != null && target.Transform.gameObject == attacker) continue;
            target.TakeDamage(shockInfo);
        }

        if (_spriteRenderer != null)
            StartCoroutine(ElectrifyVisualRoutine());
    }

    private IEnumerator ElectrifyVisualRoutine()
    {
        _spriteRenderer.color = electrifiedTint;
        yield return new WaitForSeconds(electrifiedDuration);
        _spriteRenderer.color = _originalColor;
    }
}
