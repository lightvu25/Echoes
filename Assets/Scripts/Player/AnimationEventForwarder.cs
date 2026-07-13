using UnityEngine;

/// <summary>
/// Place this script on the same GameObject as your Animator (e.g., Player Sprite).
/// It catches Animation Events and forwards them up to the root scripts.
/// </summary>
public class AnimationEventForwarder : MonoBehaviour
{
    private PlayerAttack playerAttack;

    private void Awake()
    {
        // Find the PlayerAttack script on the parent (root Player object)
        playerAttack = GetComponentInParent<PlayerAttack>();
    }

    // This is the function you will select in the Animation Event
    public void TriggerHitbox()
    {
        if (playerAttack != null)
        {
            playerAttack.TriggerHitbox();
        }
    }
}
