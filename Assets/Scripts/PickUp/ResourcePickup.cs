using UnityEngine;

public class ResourcePickup : MonoBehaviour
{
    public enum ResourceType
    {
        Gold,
        MemoryFragment,
        Exp
    }

    // ===== Config =====
    [Header("Resource")]
    [SerializeField] private ResourceType resourceType = ResourceType.Gold;
    [SerializeField] private int amount = 10;

    [Header("Magnetism")]
    [SerializeField] private float magnetRadius = 5f;
    [SerializeField] private float flySpeed = 8f;

    // ===== State =====
    private Transform playerCollectPoint;
    private bool isAttracted = false;
    private bool isCollected = false;

    private void Update()
    {
        if (isCollected) return;

        if (!isAttracted && PlayerStats.Instance != null)
        {
            playerCollectPoint = PlayerStats.Instance.collectPoint != null
                ? PlayerStats.Instance.collectPoint
                : PlayerStats.Instance.transform;

            float distance = Vector2.Distance(transform.position, playerCollectPoint.position);
            if (distance <= magnetRadius)
            {
                isAttracted = true;
            }
        }

        if (isAttracted && playerCollectPoint != null)
        {
            FlyToPlayer();
        }
    }

    private void FlyToPlayer()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            playerCollectPoint.position,
            flySpeed * Time.deltaTime
        );

        float collectDistance = Vector2.Distance(transform.position, playerCollectPoint.position);
        if (collectDistance < 0.2f)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        if (PlayerStats.Instance != null)
        {
            switch (resourceType)
            {
                case ResourceType.Gold:
                    PlayerStats.Instance.AddGold(amount);
                    break;
                case ResourceType.MemoryFragment:
                    PlayerStats.Instance.AddMemoryFragments(amount);
                    break;
                case ResourceType.Exp:
                    PlayerStats.Instance.AddExp(amount);
                    break;
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
