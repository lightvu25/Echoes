using UnityEngine;

public enum AnchorType
{
    None,
    Teleporter,
    Enemy_Ground,
    Enemy_Air,
    Echo_Common,
    Altar,
    Shrine
}

public class EntityAnchor : MonoBehaviour
{
    [Header("Anchor Configuration")]
    [Tooltip("Defines what type of entity can be injected at this location.")]
    public AnchorType anchorType;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Draw a small line indicating forward/down direction
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 0.5f);
    }

    private Color GetGizmoColor()
    {
        switch (anchorType)
        {
            case AnchorType.Teleporter: return Color.magenta;
            case AnchorType.Enemy_Ground: return Color.red;
            case AnchorType.Enemy_Air: return new Color(1f, 0.5f, 0f); // Orange
            case AnchorType.Echo_Common: return Color.cyan;
            case AnchorType.Altar: return Color.yellow;
            case AnchorType.Shrine: return Color.green;
            default: return Color.gray;
        }
    }
#endif
}
