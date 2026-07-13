using UnityEngine;
using UnityEngine.EventSystems;

public class MapCameraController : MonoBehaviour, IDragHandler, IScrollHandler
{
    [Header("Camera Settings")]
    [Tooltip("The camera that renders your minimap.")]
    [SerializeField] private Camera mapCamera;
    
    [Tooltip("The script that normally forces the camera to follow the player (e.g. MinimapPixelSnapper or a CinemachineBrain). We will disable this while the map is open.")]
    [SerializeField] private MonoBehaviour cameraFollowerScript;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 30f;

    private float originalZoom;

    private void OnEnable()
    {
        if (mapCamera != null)
        {
            // Remember original zoom
            originalZoom = mapCamera.orthographicSize;
            
            // Disable the follow script so we can freely pan the camera
            if (cameraFollowerScript != null) 
            {
                cameraFollowerScript.enabled = false;
            }
        }
    }

    private void OnDisable()
    {
        if (mapCamera != null)
        {
            // Restore original zoom and re-enable following
            mapCamera.orthographicSize = originalZoom;
            
            if (cameraFollowerScript != null) 
            {
                cameraFollowerScript.enabled = true;
            }
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (mapCamera == null) return;
        
        float scroll = eventData.scrollDelta.y;
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            mapCamera.orthographicSize -= scroll * zoomSpeed;
            mapCamera.orthographicSize = Mathf.Clamp(mapCamera.orthographicSize, minZoom, maxZoom);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mapCamera == null) return;

        // Calculate exact movement ratio so the mouse cursor stays perfectly pinned to the map as you drag
        float ratio = (2f * mapCamera.orthographicSize) / Screen.height;
        Vector3 delta = new Vector3(-eventData.delta.x, -eventData.delta.y, 0) * ratio;
        
        mapCamera.transform.position += delta;
    }
}
