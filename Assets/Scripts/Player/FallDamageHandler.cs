using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FallDamageHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float safeVelocityThreshold = -15f;
    [SerializeField] private float fatalVelocityThreshold = -35f;
    [SerializeField] private int maxFallDamage = 50;

    [Header("Stun Effect")]
    [SerializeField] private int stunDamageThreshold = 30;
    [SerializeField] private float stunDuration = 1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private IDamageable damageable;
    private PlayerMovement playerMovement;

    private float peakFallVelocity = 0f;
    public float FallDamageModifier { get; set; } = 1f;
    public bool BypassNextFallDamage { get; set; } = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        damageable = GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            damageable = GetComponentInChildren<IDamageable>();
        }
    }

    private void Update()
    {
        bool isGrounded = false;
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }

        if (!isGrounded && rb.linearVelocity.y < 0)
        {
            // Ignore velocity changes if we are currently performing a plunge attack,
            // because PlayerAttack handles plunge self-damage based on drop distance.
            if (playerMovement != null && playerMovement.isPlunging)
            {
                // Do nothing
            }
            else if (rb.linearVelocity.y < peakFallVelocity)
            {
                peakFallVelocity = rb.linearVelocity.y;
            }
        }
        else if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.1f && peakFallVelocity < 0f)
        {
            if (peakFallVelocity < safeVelocityThreshold && !BypassNextFallDamage)
            {
                float damagePercentage = Mathf.InverseLerp(safeVelocityThreshold, fatalVelocityThreshold, peakFallVelocity);
                float rawDamage = Mathf.Lerp(0, maxFallDamage, damagePercentage);
                int finalDamage = Mathf.CeilToInt(rawDamage * FallDamageModifier);

                if (finalDamage > 0 && damageable != null)
                {
                    Debug.Log($"[FallDamage] Dealt {finalDamage} damage. PeakVelocity: {peakFallVelocity}, BypassNextFallDamage: {BypassNextFallDamage}");
                    DamageInfo damageInfo = new DamageInfo
                    {
                        baseDamage = finalDamage,
                        knockbackForce = 0f,
                        knockbackDirection = Vector2.zero,
                        damageSource = DamageSourceType.FallDamage,
                        attacker = gameObject
                    };
                    damageable.TakeDamage(damageInfo);

                    if (finalDamage >= stunDamageThreshold && playerMovement != null)
                    {
                        playerMovement.ApplyStun(stunDuration);
                    }
                }
            }

            BypassNextFallDamage = false;
            peakFallVelocity = 0f;
        }
    }
}
