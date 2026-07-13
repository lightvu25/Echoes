using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MinimapPixelSnapper : MonoBehaviour
{
    [Tooltip("Set this to match your game's Pixels Per Unit (e.g., 16 or 32)")]
    [SerializeField] private float pixelsPerUnit = 16f;

    [Tooltip("The target the minimap camera should follow (usually the Player)")]
    [SerializeField] private Transform target;

    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    private void LateUpdate()
    {
        if (target == null) return;

        // Get the target's raw floating point position
        Vector3 rawPos = target.position + offset;

        // Snap the position to the nearest pixel grid based on your PPU
        float snappedX = Mathf.Round(rawPos.x * pixelsPerUnit) / pixelsPerUnit;
        float snappedY = Mathf.Round(rawPos.y * pixelsPerUnit) / pixelsPerUnit;

        // Apply the perfectly snapped position to the camera
        transform.position = new Vector3(snappedX, snappedY, rawPos.z);
    }
}
