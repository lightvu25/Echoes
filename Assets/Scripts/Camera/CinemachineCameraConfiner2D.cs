using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraConfiner2D : MonoBehaviour
{
    private CinemachineConfiner2D confiner;

    private void Awake()
    {
        confiner = GetComponent<CinemachineConfiner2D>();
        
        if (confiner == null)
        {
            confiner = GetComponentInParent<CinemachineConfiner2D>();
        }
    }

    private void OnEnable()
    {
        Room.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        Room.OnRoomEntered -= HandleRoomEntered;
    }

    private void HandleRoomEntered(Room room)
    {
        confiner.BoundingShape2D = room.CameraBoundsCollider;
        confiner.InvalidateBoundingShapeCache();
    }
}
