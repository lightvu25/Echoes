using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponVFXController : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private TrailRenderer trailRenderer;
    [Tooltip("Order should match the ElementType enum (0=None, 1=Fire, 2=Poison, 3=Lightning, 4=Ice, 5=Wind, 6=Earth)")]
    [SerializeField] private ParticleSystem[] elementParticles;

    private void Start()
    {
        if (MemoryInventorySystem.Instance != null)
        {
            MemoryInventorySystem.Instance.OnInventoryChanged += HandleInventoryChanged;

            // Trigger visual immediately to sync initial state
            HandleInventoryChanged(MemoryInventorySystem.Instance.activeSlots);
        }
    }

    private void OnDestroy()
    {
        if (MemoryInventorySystem.Instance != null)
        {
            MemoryInventorySystem.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged(IReadOnlyList<MemoryItemData> activeSlots)
    {
        ElementType dominantElement = ElementType.None;

        foreach (var slot in activeSlots)
        {
            if (slot != null && slot.elementType != ElementType.None)
            {
                dominantElement = slot.elementType;
                break;
            }
        }

        UpdateVFXForElement(dominantElement);
    }

    private void UpdateVFXForElement(ElementType elementType)
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
            switch (elementType)
            {
                case ElementType.Fire:
                    trailRenderer.startColor = Color.red;
                    trailRenderer.endColor = new Color(1f, 0f, 0f, 0f);
                    break;
                case ElementType.Poison:
                    trailRenderer.startColor = Color.green;
                    trailRenderer.endColor = new Color(0f, 1f, 0f, 0f);
                    break;
                case ElementType.Lightning:
                    trailRenderer.startColor = Color.yellow;
                    trailRenderer.endColor = new Color(1f, 0.9f, 0f, 0f);
                    break;
                case ElementType.Ice:
                    trailRenderer.startColor = Color.cyan;
                    trailRenderer.endColor = new Color(0f, 1f, 1f, 0f);
                    break;
                case ElementType.Wind:
                    trailRenderer.startColor = Color.white;
                    trailRenderer.endColor = new Color(1f, 1f, 1f, 0f);
                    break;
                case ElementType.Earth:
                    trailRenderer.startColor = new Color(0.6f, 0.3f, 0f, 1f);
                    trailRenderer.endColor = new Color(0.6f, 0.3f, 0f, 0f);
                    break;
                case ElementType.None:
                default:
                    trailRenderer.startColor = Color.white;
                    trailRenderer.endColor = new Color(1f, 1f, 1f, 0f);
                    break;
            }
        }

        // Enable matching particle system (by enum index)
        int elementIndex = (int)elementType;
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
