using UnityEngine;

public class VFXFollower : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;

    public void Initialize(Transform targetTransform, Vector3 followOffset)
    {
        target = targetTransform;
        offset = followOffset;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}
