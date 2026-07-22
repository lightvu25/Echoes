using UnityEngine;
using System;

public class PickupVisual : MonoBehaviour
{
    [SerializeField] private ParticleSystem particlePickupPrefab;

    private void Start()
    {
        PlayerInteract.Instance.OnCoinPickup += HandleCoinPickup;
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
        PlayPickupEffect(e.coinPickup.transform.position);
    }



    private void PlayPickupEffect(Vector3 position)
    {
        Instantiate(particlePickupPrefab, position, Quaternion.identity);
    }
}
