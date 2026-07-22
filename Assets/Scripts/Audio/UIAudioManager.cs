using UnityEngine;

public class UIAudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("Music Settings")]
    [SerializeField] private bool duckMusicOnOpen = true;

    private void OnEnable()
    {
        if (openSound != null)
        {
            SoundManager.Instance.PlaySFX(openSound, sfxVolume);
        }

        if (duckMusicOnOpen)
        {
            MusicManager.Instance.DuckMusic(true);
        }
    }

    private void OnDisable()
    {
        if (closeSound != null)
        {
            SoundManager.Instance.PlaySFX(closeSound, sfxVolume);
        }

        if (duckMusicOnOpen)
        {
            MusicManager.Instance.DuckMusic(false);
        }
    }
}