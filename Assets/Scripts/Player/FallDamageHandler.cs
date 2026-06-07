using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FallDamageHandler : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float safeVelocityThreshold = -15f;
    [SerializeField] private float fatalVelocityThreshold = -35f;
    [SerializeField] private int maxFallDamage = 50;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private IDamageable damageable;

    private float peakFallVelocity = 0f;
    public float FallDamageModifier { get; set; } = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
            if (rb.linearVelocity.y < peakFallVelocity)
            {
                peakFallVelocity = rb.linearVelocity.y;
            }
        }
        else if (isGrounded && peakFallVelocity < 0f)
        {
            if (peakFallVelocity < safeVelocityThreshold)
            {
                float damagePercentage = Mathf.InverseLerp(safeVelocityThreshold, fatalVelocityThreshold, peakFallVelocity);
                float rawDamage = Mathf.Lerp(0, maxFallDamage, damagePercentage);
                int finalDamage = Mathf.CeilToInt(rawDamage * FallDamageModifier);

                if (finalDamage > 0 && damageable != null)
                {
                    DamageInfo damageInfo = new DamageInfo
                    {
                        baseDamage = finalDamage,
                        knockbackForce = 0f,
                        knockbackDirection = Vector2.zero,
                        damageSource = "FallDamage",
                        attacker = gameObject
                    };
                    damageable.TakeDamage(damageInfo);
                }
            }

            peakFallVelocity = 0f;
        }
    }
}
