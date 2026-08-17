using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Smoothly places the player toward the opposite side of the screen from the
/// direction they are facing, revealing more of the path ahead.
/// </summary>
[RequireComponent(typeof(CinemachinePositionComposer))]
[RequireComponent(typeof(CinemachineCamera))]
public sealed class CinemachineFacingLookAhead2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachinePositionComposer positionComposer;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Look Ahead")]
    [Tooltip("Distance from screen center. Cinemachine uses 0.5 as the screen edge.")]
    [SerializeField, Range(0f, 0.4f)] private float horizontalScreenOffset = 0.18f;
    [Tooltip("Time taken to move the framing from one facing direction to the other.")]
    [SerializeField, Min(0.01f)] private float transitionSmoothTime = 0.35f;

    private float originalScreenPositionX;
    private float screenPositionVelocity;
    private bool hasCapturedOriginalPosition;
    private Transform resolvedTrackingTarget;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        hasCapturedOriginalPosition = false;
        CaptureOriginalScreenPosition();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (positionComposer == null || playerMovement == null) return;

        CaptureOriginalScreenPosition();

        // Facing right places the player left of center, revealing more space on the right.
        float facingSign = playerMovement.isFacingRight ? 1f : -1f;
        float targetScreenPositionX = originalScreenPositionX - facingSign * horizontalScreenOffset;

        ScreenComposerSettings composition = positionComposer.Composition;
        composition.ScreenPosition.x = Mathf.SmoothDamp(
            composition.ScreenPosition.x,
            targetScreenPositionX,
            ref screenPositionVelocity,
            transitionSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);
        positionComposer.Composition = composition;
    }

    private void OnDisable()
    {
        if (positionComposer == null || !hasCapturedOriginalPosition) return;

        ScreenComposerSettings composition = positionComposer.Composition;
        composition.ScreenPosition.x = originalScreenPositionX;
        positionComposer.Composition = composition;
        screenPositionVelocity = 0f;
        hasCapturedOriginalPosition = false;
    }

    private void ResolveReferences()
    {
        if (positionComposer == null)
        {
            positionComposer = GetComponent<CinemachinePositionComposer>();
        }

        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        Transform trackingTarget = cinemachineCamera != null
            ? cinemachineCamera.Target.TrackingTarget
            : null;
        if (trackingTarget != resolvedTrackingTarget)
        {
            resolvedTrackingTarget = trackingTarget;
            playerMovement = null;
            if (trackingTarget == null) return;

            playerMovement = trackingTarget.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = trackingTarget.GetComponentInParent<PlayerMovement>();
            }
        }
    }

    private void CaptureOriginalScreenPosition()
    {
        if (hasCapturedOriginalPosition || positionComposer == null) return;

        originalScreenPositionX = positionComposer.Composition.ScreenPosition.x;
        hasCapturedOriginalPosition = true;
    }

    private void OnValidate()
    {
        horizontalScreenOffset = Mathf.Clamp(horizontalScreenOffset, 0f, 0.4f);
        transitionSmoothTime = Mathf.Max(0.01f, transitionSmoothTime);

        if (positionComposer == null)
        {
            positionComposer = GetComponent<CinemachinePositionComposer>();
        }

        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }
    }
}
