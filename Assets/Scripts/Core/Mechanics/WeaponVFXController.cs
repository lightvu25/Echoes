using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponVFXController : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private TrailRenderer trailRenderer;
    [Tooltip("Order should match the EchoType enum (0=None, 1=Fire, 2=Poison, 3=Lightning, 4=Ice, 5=Wind, 6=Earth)")]
    [SerializeField] private ParticleSystem[] elementParticles;

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
        EchoType dominantElement = EchoType.None;

        var activeElement = PlayerInventoryCore.Instance.ActiveElement;
        if (activeElement != null && activeElement.echoType != EchoType.None)
        {
            dominantElement = activeElement.echoType;
        }
        else 
        {
            var allTypes = PlayerInventoryCore.Instance.GetAllActiveElementTypes();
            if (allTypes.Count > 0)
            {
                dominantElement = allTypes[0];
            }
        }

        UpdateVFXForElement(dominantElement);
    }

    private void UpdateVFXForElement(EchoType EchoType)
    {
        // Stop all active element particles
        if (elementParticles != null)
        {
            foreach (var ps in elementParticles)
            {
                if (ps != null && ps.isPlaying)
                {
                    ps.Stop();
                }
            }
        }

        // Color the trail based on the dominant element
        if (trailRenderer != null)
        {
            switch (EchoType)
            {
                case EchoType.Blaze:
                    trailRenderer.startColor = Color.red;
                    trailRenderer.endColor = new Color(1f, 0f, 0f, 0f);
                    break;
                case EchoType.Frostbite:
                    trailRenderer.startColor = Color.cyan;
                    trailRenderer.endColor = new Color(0f, 1f, 1f, 0f);
                    break;
                case EchoType.Arc:
                    trailRenderer.startColor = Color.yellow;
                    trailRenderer.endColor = new Color(1f, 0.9f, 0f, 0f);
                    break;
                case EchoType.Kinetic:
                    trailRenderer.startColor = Color.white;
                    trailRenderer.endColor = new Color(1f, 1f, 1f, 0f);
                    break;
                case EchoType.Anomaly:
                    trailRenderer.startColor = Color.green;
                    trailRenderer.endColor = new Color(0f, 1f, 0f, 0f);
                    break;
                case EchoType.Void:
                    trailRenderer.startColor = new Color(0.5f, 0f, 0.5f, 1f); // Purple
                    trailRenderer.endColor = new Color(0.5f, 0f, 0.5f, 0f);
                    break;
                case EchoType.Curse:
                    trailRenderer.startColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark
                    trailRenderer.endColor = new Color(0.1f, 0.1f, 0.1f, 0f);
                    break;
                case EchoType.None:
                default:
                    trailRenderer.startColor = Color.white;
                    trailRenderer.endColor = new Color(1f, 1f, 1f, 0f);
                    break;
            }
        }

        // Enable matching particle system (by enum index)
        int elementIndex = (int)EchoType;
        if (elementParticles != null && elementIndex >= 0 && elementIndex < elementParticles.Length)
        {
            var targetPS = elementParticles[elementIndex];
            if (targetPS != null && !targetPS.isPlaying)
            {
                targetPS.Play();
            }
        }
    }
}
