using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class CutsceneLockPlayer : MonoBehaviour
{
    private PlayableDirector director;

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    private void OnEnable()
    {
        director.played += OnPlay;
        director.stopped += OnStop;
    }

    private void OnDisable()
    {
        director.played -= OnPlay;
        director.stopped -= OnStop;
    }

    private void OnPlay(PlayableDirector d)
    {
        // Disable all player input
        if (GameInput.Instance != null)
        {
            GameInput.Instance.SetInputsEnabled(false);
        }

        // Also force the player's movement physics to stop immediately
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (player.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    }

    private void OnStop(PlayableDirector d)
    {
        // Re-enable player input once the cutscene finishes
        if (GameInput.Instance != null)
        {
            GameInput.Instance.SetInputsEnabled(true);
        }
    }
}
