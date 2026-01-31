using UnityEngine;
using Unity.Cinemachine;

public class CinemachineCameraZoom2D : MonoBehaviour
{
    public static CinemachineCameraZoom2D Instance { get; private set; }

    [SerializeField] private float NORMAL_ORTHOGRAPHIC_SIZE;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private float targetOrthographicSize;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        float zoomSpeed = 2f;
        cinemachineCamera.Lens.OrthographicSize = Mathf.Lerp(cinemachineCamera.Lens.OrthographicSize, targetOrthographicSize, Time.deltaTime * zoomSpeed);
    }

    public void SetTargetOrthographicSize(float targetOrthographicSize)
    {
        this.targetOrthographicSize = targetOrthographicSize;
    }

    public void SetNormalOrthographicSize()
    {
        SetTargetOrthographicSize(NORMAL_ORTHOGRAPHIC_SIZE);
    }
}
