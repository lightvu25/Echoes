using UnityEngine;
using System;

public class PickupVisual : MonoBehaviour
{
    [SerializeField] private ParticleSystem particlePickupPrefab;

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
        PlayPickupEffect(e.coinPickup.transform.position);
    }

    private void HandleTimePickup(object sender, PlayerInteract.OnTimePickupEventArgs e)
    {
        PlayPickupEffect(e.timePickup.transform.position);
    }

    private void PlayPickupEffect(Vector3 position)
    {
        Instantiate(particlePickupPrefab, position, Quaternion.identity);
    }
}
