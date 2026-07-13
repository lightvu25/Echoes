using UnityEngine;

public class Goal : MonoBehaviour
{
    public void Interact()
    {
        if (PlayerInteract.Instance != null)
        {
            PlayerInteract.Instance.TriggerGoal(this);
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
