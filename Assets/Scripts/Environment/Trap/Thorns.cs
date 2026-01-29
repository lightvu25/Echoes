using UnityEngine;

public class Thorns : MonoBehaviour
{
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    if (collision.TryGetComponent(out PlayerInteract player))
    //    {
    //        player.Dead();
    //    }
    //}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out PlayerInteract player))
        {
            player.Dead();
        }
    }
}
