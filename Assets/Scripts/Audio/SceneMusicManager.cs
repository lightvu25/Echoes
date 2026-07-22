using UnityEngine;

public class SceneMusicManager : MonoBehaviour
{
    [Header("Map Music Settings")]
    public string defaultMusicTrack = "The Abyss";
    public string defaultAmbientTrack = "Abyss Ambient";

    private void Start()
    {
        if (!string.IsNullOrEmpty(defaultMusicTrack))
            MusicManager.Instance.PlayMusic(defaultMusicTrack, 1f);

        if (!string.IsNullOrEmpty(defaultAmbientTrack))
            MusicManager.Instance.PlayAmbient(defaultAmbientTrack, 1f);
    }
}