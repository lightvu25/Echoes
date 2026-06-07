using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutsceneController : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Trigger Mode
    // -----------------------------------------------------------------------

    [Flags]
    public enum TriggerMode
    {
        None          = 0,
        PlayOnStart   = 1 << 0,
        PlayOnTrigger = 1 << 1,
        PlayOnEvent   = 1 << 2,
    }

    // -----------------------------------------------------------------------
    // Inspector
    // -----------------------------------------------------------------------

    [Header("Trigger")]
    [SerializeField] private TriggerMode triggerMode = TriggerMode.PlayOnStart;
    [Tooltip("Tag that activates the trigger zone (PlayOnTrigger only).")]
    [SerializeField] private string triggerTag = "Player";

    [Header("Directors")]
    [Tooltip("Ordered list of PlayableDirectors to play sequentially.")]
    [SerializeField] private List<PlayableDirector> directors = new();

    [Header("After Cutscene")]
    [Tooltip("Load a new scene when the last director finishes.")]
    [SerializeField] private bool loadSceneAfterCutscene = false;
    [SerializeField] private string nextSceneName;

    // -----------------------------------------------------------------------
    // State
    // -----------------------------------------------------------------------

    private bool _hasPlayed;

    // Cached player components for locking
    private PlayerMovement _playerMovement;
    private PlayerAttack   _playerAttack;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    private void Start()
    {
        CachePlayerComponents();

        if (triggerMode.HasFlag(TriggerMode.PlayOnStart))
            TryPlay();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerMode.HasFlag(TriggerMode.PlayOnTrigger)) return;
        if (!other.CompareTag(triggerTag)) return;
        TryPlay();
    }

    /// <summary>Call from any external event (e.g. boss death, EventManager).</summary>
    public void PlayFromEvent()
    {
        if (!triggerMode.HasFlag(TriggerMode.PlayOnEvent)) return;
        TryPlay();
    }

    // -----------------------------------------------------------------------
    // Core Logic
    // -----------------------------------------------------------------------

    private void TryPlay()
    {
        if (_hasPlayed || directors == null || directors.Count == 0) return;
        _hasPlayed = true;

        LockPlayer(true);
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        foreach (PlayableDirector director in directors)
        {
            if (director == null) continue;

            director.Play();
            director.stopped += OnDirectorStopped;

            // Wait until this director finishes before moving to the next
            yield return new WaitUntil(() => director.state != PlayState.Playing);

            director.stopped -= OnDirectorStopped; // unsubscribe immediately — no leaks
        }

        OnAllDirectorsFinished();
    }

    /// <summary>
    /// Fired when a director stops (externally or naturally).
    /// WaitUntil in PlaySequence resolves this automatically.
    /// Kept as an explicit handler for clarity and safe unsubscription.
    /// </summary>
    private void OnDirectorStopped(PlayableDirector director) { }

    private void OnAllDirectorsFinished()
    {
        LockPlayer(false);

        if (loadSceneAfterCutscene && !string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // -----------------------------------------------------------------------
    // Player Locking
    // -----------------------------------------------------------------------

    private void CachePlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        _playerMovement = player.GetComponent<PlayerMovement>();
        _playerAttack   = player.GetComponent<PlayerAttack>();
    }

    private void LockPlayer(bool locked)
    {
        // Re-cache if player wasn't available at Start (spawned later)
        if (_playerMovement == null) CachePlayerComponents();

        if (_playerMovement != null)
        {
            _playerMovement.enabled = !locked;

            // Zero velocity so the player doesn't drift during the cutscene
            if (locked && _playerMovement.rb != null)
                _playerMovement.rb.linearVelocity = Vector2.zero;
        }

        if (_playerAttack != null)
            _playerAttack.enabled = !locked;
    }

    // -----------------------------------------------------------------------
    // Skip (wire to a UI button)
    // -----------------------------------------------------------------------

    /// <summary>Immediately stops all directors and ends the cutscene.</summary>
    public void Skip()
    {
        StopAllCoroutines();

        foreach (PlayableDirector d in directors)
        {
            if (d == null) continue;
            d.stopped -= OnDirectorStopped;
            d.Stop();
        }

        OnAllDirectorsFinished();
    }
}
