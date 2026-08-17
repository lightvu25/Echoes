using UnityEngine;

namespace Echoes.Audio
{
    /// <summary>
    /// A generic component that plays an AudioClip via the SoundManager.
    /// This is designed to be hooked up to UnityEvents (like InteractableTrigger's onInteract).
    /// </summary>
    public class EventAudioPlayer : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioClip soundClip;
        [Range(0f, 1f)] [SerializeField] private float volume = 1f;
        [SerializeField] private bool useRandomPitch = true;

        /// <summary>
        /// Plays the assigned sound clip using the global SoundManager.
        /// Call this from a UnityEvent.
        /// </summary>
        public void PlaySound()
        {
            if (soundClip != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(soundClip, volume, useRandomPitch);
            }
        }
    }
}
