using UnityEngine;

public class Thorns : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Damage dealt as a fraction of the player's MaxHP (0–1). Default = 30 %.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float damagePercent = 0.30f;
    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 8f;
    [Header("Rate Limiting")]
    [Tooltip("Minimum seconds between consecutive damage ticks to the same player.")]
    [SerializeField] private float damageCooldown = 0.5f;
    private float _nextDamageTime;
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < _nextDamageTime) return;
        if (!collision.gameObject.TryGetComponent(out PlayerCombat playerCombat)) return;
        
        if (playerCombat.IsDead) return;
        
        int damageAmount = Mathf.CeilToInt(playerCombat.MaxHP * damagePercent);
        
        
        Vector2 toPlayer = (Vector2)(playerCombat.Transform.position - transform.position);
        Vector2 knockbackDir = (toPlayer == Vector2.zero) ? Vector2.up : toPlayer.normalized;
        
        DamageInfo damageInfo = DamageInfo.CreateWithKnockback(
            damageAmount,
            gameObject,
            knockbackDir,
            knockbackForce
        );
        damageInfo.damageSource = DamageSourceType.Environment_Thorns;
        
        playerCombat.TakeDamage(damageInfo);
        _nextDamageTime = Time.time + damageCooldown;
    }
}
