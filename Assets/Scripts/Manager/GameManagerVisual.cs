using Unity.Cinemachine;
using UnityEngine;

public class GameManagerVisual : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource pickupCinemachineImpulseSource;
    [SerializeField] private Transform pickupCollectVfxPrefab;
    // [SerializeField] private ScorePopup scorePopupPrefab;
    [SerializeField] private Transform confettiVfxPrefab;

    private void Start()
    {
        PlayerInteract.Instance.OnCoinPickup += PlayerInteract_OnCoinPickup;
        PlayerInteract.Instance.OnTimePickup += PlayerInteract_OnTimePickup;
        PlayerInteract.Instance.OnGoal += PlayerInteract_OnGoal;
    }

    private void PlayerInteract_OnCoinPickup(object sender, PlayerInteract.OnCoinPickupEventArgs e)
    {
        Transform pickupCollectVfxTransform = Instantiate(pickupCollectVfxPrefab, e.coinPickup.transform.position, Quaternion.identity);
        Destroy(pickupCollectVfxTransform.gameObject, 1f);
        pickupCinemachineImpulseSource.GenerateImpulse(4f);
        // Instantiate(scorePopupPrefab, e.coin.transform.position, Quaternion.identity);
    }

    private void PlayerInteract_OnTimePickup(object sender, PlayerInteract.OnTimePickupEventArgs e)
    {
        Transform timePickupCollectVfxTransform = Instantiate(pickupCollectVfxPrefab, e.timePickup.transform.position, Quaternion.identity);
        Destroy(timePickupCollectVfxTransform.gameObject, 1f);
        pickupCinemachineImpulseSource.GenerateImpulse(3f);
    }

    private void PlayerInteract_OnGoal(object sender, PlayerInteract.OnGoalEventArgs e)
    {
        Transform goalCollectVfxTransform = Instantiate(pickupCollectVfxPrefab, e.goal.transform.position, Quaternion.identity);
        Destroy(goalCollectVfxTransform.gameObject, 1f);
        pickupCinemachineImpulseSource.GenerateImpulse(4f);
    }
}