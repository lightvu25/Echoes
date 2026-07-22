using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Handles pickup-driven camera shakes by listening to <see cref="PlayerInteract"/>
/// events and forwarding impulse generation to <see cref="GameFeelManager"/>.
///
/// ARCHITECTURAL NOTE:
///   All camera shake now routes through <see cref="GameFeelManager.GenerateShake"/>,
///   which owns the <see cref="CinemachineImpulseSource"/>.  This class only
///   listens to pickup events and asks the manager to shake — it does NOT own
///   an impulse source of its own, to avoid duplicate shake components and
///   inconsistent feel profiles.
/// </summary>
public class CinemachineCameraShake2D : MonoBehaviour
{
    public static CinemachineCameraShake2D Instance { get; private set; }

    [Tooltip("Shake intensity applied on coin or time pickup.")]
    [SerializeField] private float pickupShakeForce = 0.2f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.OnCoinPickup += HandleCoinPickup;
        }
    }

    private void OnDestroy()
    {
        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.OnCoinPickup -= HandleCoinPickup;
        }
    }

    private void HandleCoinPickup(object sender, PlayerInteract.OnCoinPickupEventArgs e)
    {
        ShakeCamera(pickupShakeForce);
    }



    /// <summary>
    /// Forwards a shake request to <see cref="GameFeelManager"/>.
    /// All impulse generation is centralised there.
    /// </summary>
    public void ShakeCamera(float force)
    {
        GameFeelManager.Instance?.GenerateShake(force);
    }
}
