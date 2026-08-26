using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using TMPro;

public class CutsceneManager : MonoBehaviour
{
    [Header("Opening Cutscene")]
    public PlayableDirector openingDirector;

    [Header("Death Cutscene")]
    public PlayableDirector deathDirector;
    public TMP_Text shardSubTitleText;

    [Header("Goal Cutscene")]
    public PlayableDirector goalDirector;

    private readonly HashSet<PlayableDirector> subscribedDirectors = new HashSet<PlayableDirector>();
    private readonly HashSet<PlayableDirector> activeDirectors = new HashSet<PlayableDirector>();
    private Camera cutsceneCamera;
    private int cameraCullingMaskBeforeCutscene;
    private bool cutsceneCullingMaskApplied;

    private void OnEnable()
    {
        SubscribeDirector(openingDirector);
        SubscribeDirector(deathDirector);
        SubscribeDirector(goalDirector);
    }

    private void OnDisable()
    {
        foreach (PlayableDirector director in subscribedDirectors)
        {
            if (director == null) continue;
            director.played -= HandleDirectorPlayed;
            director.stopped -= HandleDirectorStopped;
        }

        subscribedDirectors.Clear();
        activeDirectors.Clear();
        RestoreCameraCullingMask();
    }

    private void Start()
    {
        if (openingDirector != null)
        {
            openingDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            if (openingDirector.state == PlayState.Playing)
                HandleDirectorPlayed(openingDirector);
            if (openingDirector.state != PlayState.Playing && !openingDirector.playOnAwake)
                openingDirector.gameObject.SetActive(false);
        }

        if (deathDirector != null)
        {
            deathDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            if (deathDirector.state == PlayState.Playing)
                HandleDirectorPlayed(deathDirector);
            if (deathDirector.state != PlayState.Playing && !deathDirector.playOnAwake)
                deathDirector.gameObject.SetActive(false);
        }

        if (goalDirector != null)
        {
            goalDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            if (goalDirector.state == PlayState.Playing)
                HandleDirectorPlayed(goalDirector);
            if (goalDirector.state != PlayState.Playing && !goalDirector.playOnAwake)
                goalDirector.gameObject.SetActive(false);
        }
    }

    private void SubscribeDirector(PlayableDirector director)
    {
        if (director == null || !subscribedDirectors.Add(director)) return;

        director.played += HandleDirectorPlayed;
        director.stopped += HandleDirectorStopped;
    }

    private void HandleDirectorPlayed(PlayableDirector director)
    {
        if (director == null || !activeDirectors.Add(director)) return;
        if (activeDirectors.Count == 1)
            ApplyCutsceneCullingMask();
    }

    private void HandleDirectorStopped(PlayableDirector director)
    {
        if (director != null)
            activeDirectors.Remove(director);

        if (activeDirectors.Count == 0)
            RestoreCameraCullingMask();
    }

    private void ApplyCutsceneCullingMask()
    {
        if (cutsceneCullingMaskApplied) return;

        cutsceneCamera = Camera.main;
        if (cutsceneCamera == null)
        {
            Debug.LogWarning("[CutsceneManager] No Main Camera was found, so cutscene layer isolation could not be applied.", this);
            return;
        }

        int visibleLayers = LayerMask.GetMask("UI", "Player");
        if (visibleLayers == 0)
        {
            Debug.LogWarning("[CutsceneManager] The UI and Player layers could not be resolved.", this);
            return;
        }

        cameraCullingMaskBeforeCutscene = cutsceneCamera.cullingMask;
        cutsceneCamera.cullingMask = visibleLayers;
        cutsceneCullingMaskApplied = true;
    }

    private void RestoreCameraCullingMask()
    {
        if (!cutsceneCullingMaskApplied) return;

        if (cutsceneCamera != null)
            cutsceneCamera.cullingMask = cameraCullingMaskBeforeCutscene;

        cutsceneCamera = null;
        cutsceneCullingMaskApplied = false;
    }

    private PlayerMovement _playerMovement;
    private PlayerAttack _playerAttack;
    private Rigidbody2D _playerRb;
    private Collider2D _playerCollider;
    private MindPlayerMovement _mindPlayerMovement;

    private void CachePlayerComponents()
    {
        _playerMovement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
        _mindPlayerMovement = FindAnyObjectByType<MindPlayerMovement>(FindObjectsInactive.Exclude);

        GameObject player = _playerMovement != null
            ? _playerMovement.gameObject
            : _mindPlayerMovement != null
                ? _mindPlayerMovement.gameObject
                : null;

        if (player == null)
        {
            return;
        }

        _playerAttack = player.GetComponent<PlayerAttack>();
        _playerRb = player.GetComponent<Rigidbody2D>();
        _playerCollider = player.GetComponent<Collider2D>();
    }

    private void LockPlayer(bool locked)
    {
        if (_playerMovement == null && _mindPlayerMovement == null) CachePlayerComponents();

        if (_playerMovement != null)
            _playerMovement.enabled = !locked;

        if (_playerAttack != null)
            _playerAttack.enabled = !locked;
            
        if (_mindPlayerMovement != null)
            _mindPlayerMovement.enabled = !locked;

        if (GameInput.Instance != null)
            GameInput.Instance.SetInputsEnabled(!locked);
    }

    public IEnumerator PlayOpeningSequence()
    {
        if (openingDirector == null) yield break;

        LockPlayer(true);

        openingDirector.gameObject.SetActive(true);
        openingDirector.Play();
        yield return StartCoroutine(WaitForDirectorOrSkip(openingDirector));
        openingDirector.gameObject.SetActive(false);
        HandleDirectorStopped(openingDirector);

        LockPlayer(false);
    }

    public IEnumerator PlayDeathSequence(int lostShards)
    {
        if (deathDirector == null)
        {
            Debug.LogError("[CutsceneManager] Cannot play the death sequence because no death director is assigned.", this);
            yield break;
        }

        if (shardSubTitleText != null)
        {
            shardSubTitleText.text = $"You lost {lostShards} Shards";
        }

        LockPlayer(true);

        if (_playerRb != null)
        {
            _playerRb.linearVelocity = new Vector2(0, _playerRb.linearVelocity.y);
        }

        try
        {
            deathDirector.gameObject.SetActive(true);
            deathDirector.Stop();
            deathDirector.time = 0d;
            deathDirector.Evaluate();
            deathDirector.Play();
            yield return StartCoroutine(WaitForDirectorOrSkip(deathDirector));

            yield return new WaitUntil(() => Input.anyKeyDown);
        }
        finally
        {
            deathDirector.Stop();
            deathDirector.time = 0d;
            deathDirector.gameObject.SetActive(false);
            HandleDirectorStopped(deathDirector);

            if (TimeManager.Instance != null) TimeManager.Instance.ClearAllPauses();
            else Time.timeScale = 1f;
            LockPlayer(false);
        }
    }

    public IEnumerator PlayGoalSequence(Vector3 goalPosition)
    {
        LockPlayer(true);

        if (_playerRb != null)
        {
            float originalGravity = _playerRb.gravityScale;
            _playerRb.gravityScale = 0f;
            _playerRb.linearVelocity = Vector2.zero;

            float time = 0f;
            float duration = 0.5f;
            Vector3 startPos = _playerRb.position;
            
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = time / duration;
                _playerRb.position = new Vector2(Mathf.Lerp(startPos.x, goalPosition.x, t), startPos.y);
                yield return null;
            }

            _playerRb.position = new Vector2(goalPosition.x, startPos.y);
            _playerRb.gravityScale = originalGravity;
            _playerRb.linearVelocity = new Vector2(0, -15f);
        }

        if (goalDirector != null)
        {
            goalDirector.gameObject.SetActive(true);
            goalDirector.Play();
            yield return StartCoroutine(WaitForDirectorOrSkip(goalDirector));
            goalDirector.gameObject.SetActive(false);
            HandleDirectorStopped(goalDirector);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        LockPlayer(false);
    }

    private IEnumerator WaitForDirectorOrSkip(PlayableDirector director)
    {
        if (director == null) yield break;

        float lastInputTime = -10f;
        float doubleTapThreshold = 0.5f;

        // Wait a frame in case Play() hasn't updated the state yet
        yield return null;

        // Wait strictly until the director finishes playing
        while (director.state == PlayState.Playing)
        {
            if (Input.anyKeyDown)
            {
                if (Time.unscaledTime - lastInputTime < doubleTapThreshold)
                {
                    // Double tap detected! Fast-forward cutscene.
                    director.time = director.duration;
                    director.Evaluate();
                    director.Stop(); // Force state to end
                    break;
                }
                lastInputTime = Time.unscaledTime;
            }

            yield return null;
        }
    }
}
