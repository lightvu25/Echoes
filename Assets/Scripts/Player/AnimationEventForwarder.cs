using UnityEngine;

/// <summary>
/// Place this script on the same GameObject as your Animator (e.g., Player Sprite).
/// It catches Animation Events and forwards them up to the root scripts.
/// </summary>
public class AnimationEventForwarder : MonoBehaviour
{
    private PlayerAttack playerAttack;
    private EntityAudioManager audioManager;

    private void Awake()
    {
        // Find the PlayerAttack script on the parent (root Player object)
        playerAttack = GetComponentInParent<PlayerAttack>();
        
        // Find the EntityAudioManager (could be on a sibling object like PlayerAudio or on the parent)
        if (transform.parent != null)
            audioManager = transform.parent.GetComponentInChildren<EntityAudioManager>();
        else
            audioManager = GetComponentInChildren<EntityAudioManager>();
    }

    // This is the function you will select in the Animation Event
    public void TriggerHitbox()
    {
        if (playerAttack != null)
        {
            playerAttack.TriggerHitbox();
        }
    }

    // Select this function in the Animation Event and pass the audio ID (e.g., "Run")
    public void PlaySound(string soundId)
    {
        if (audioManager != null)
        {
            audioManager.PlaySound(soundId);
        }
        else
        {
            Debug.LogWarning("AnimationEventForwarder: No EntityAudioManager found to play " + soundId);
        }
    }
}
