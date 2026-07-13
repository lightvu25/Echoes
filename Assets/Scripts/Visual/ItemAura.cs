using UnityEngine;

public class ItemAura : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = -90f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.05f;

    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = initialScale + new Vector3(scaleOffset, scaleOffset, 0f);
    }
}