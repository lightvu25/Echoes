using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraShake2D : MonoBehaviour
{
    public static CinemachineCameraShake2D Instance { get; private set; }

    private CinemachineImpulseSource impulseSource;

    [SerializeField] private float pickupShakeForce = 0.2f;
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private void Awake()
    {
        Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Start()
    {
        PlayerInteract.Instance.OnCoinPickup += HandleCoinPickup;
        PlayerInteract.Instance.OnTimePickup += HandleTimePickup;
    }

    private void OnDestroy()
    {
        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.OnCoinPickup -= HandleCoinPickup;
            PlayerInteract.Instance.OnTimePickup -= HandleTimePickup;
        }
    }

    private void HandleCoinPickup(object sender, PlayerInteract.OnCoinPickupEventArgs e)
    {
        ShakeCamera(pickupShakeForce);
    }

    private void HandleTimePickup(object sender, PlayerInteract.OnTimePickupEventArgs e)
    {
        ShakeCamera(pickupShakeForce);
    }

    private void ShakeCamera(float force)
    {
        impulseSource.GenerateImpulse(force);
    }
}
