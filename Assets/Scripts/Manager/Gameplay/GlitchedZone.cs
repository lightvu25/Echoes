using UnityEngine;

public class GlitchedZone : MonoBehaviour
{
    private void Start()
    {
        Destroy(gameObject, 5f);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy") || collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            EchoStatusReceiver status = collision.GetComponent<EchoStatusReceiver>();
            if (status != null)
            {
                status.ApplySilence(0.5f);
            }
        }
    }
}
