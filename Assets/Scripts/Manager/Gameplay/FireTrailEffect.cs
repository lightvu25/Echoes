using UnityEngine;

public class FireTrailEffect : MonoBehaviour
{
    private void Start()
    {
        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            EchoStatusReceiver status = collision.GetComponentInParent<EchoStatusReceiver>();
            if (status != null)
            {
                status.ApplyBurn(4f);
            }
        }
    }
}
