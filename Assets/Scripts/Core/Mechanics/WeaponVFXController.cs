using UnityEngine;

public class WeaponVFXController : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private TrailRenderer trailRenderer;

    private void Start()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged += HandleInventoryChanged;

            // Trigger visual immediately to sync initial state
            HandleInventoryChanged();
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventoryCore.Instance != null)
        {
            PlayerInventoryCore.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        Color trailColor = Color.white; // Default color

        if (PlayerInventoryCore.Instance != null)
        {
            EchoData activeEcho = PlayerInventoryCore.Instance.GetActiveEcho();
            if (activeEcho != null && activeEcho.echoType != EchoType.None)
            {
                trailColor = activeEcho.trailColor;
            }
        }

        if (trailRenderer != null)
        {
            trailRenderer.startColor = trailColor;
            trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        }
    }
}
