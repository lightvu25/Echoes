using System.Collections;
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

    private void Start()
    {
        if (openingDirector != null)
        {
            openingDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            if (openingDirector.state != PlayState.Playing && !openingDirector.playOnAwake)
                openingDirector.gameObject.SetActive(false);
        }

        if (deathDirector != null)
        {
            deathDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            if (deathDirector.state != PlayState.Playing && !deathDirector.playOnAwake)
                deathDirector.gameObject.SetActive(false);
        }

        if (goalDirector != null)
        {
            goalDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            if (goalDirector.state != PlayState.Playing && !goalDirector.playOnAwake)
                goalDirector.gameObject.SetActive(false);
        }
    }

    private PlayerMovement _playerMovement;
    private PlayerAttack _playerAttack;
    private Rigidbody2D _playerRb;
    private Collider2D _playerCollider;
    private MindPlayerMovement _mindPlayerMovement;

    private void CachePlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        _playerMovement = player.GetComponent<PlayerMovement>();
        _playerAttack = player.GetComponent<PlayerAttack>();
        _playerRb = player.GetComponent<Rigidbody2D>();
        _playerCollider = player.GetComponent<Collider2D>();
        _mindPlayerMovement = player.GetComponent<MindPlayerMovement>();
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

        LockPlayer(false);
    }

    public IEnumerator PlayDeathSequence(int lostShards)
    {
        if (shardSubTitleText != null)
        {
            shardSubTitleText.text = $"You lost {lostShards} Shards";
        }

        LockPlayer(true);

        if (_playerRb != null)
        {
            _playerRb.linearVelocity = new Vector2(0, _playerRb.linearVelocity.y);
        }

        // Freeze the world completely so enemies and physics stop
        // Time.timeScale = 0f;

        if (deathDirector != null)
        {
            deathDirector.gameObject.SetActive(true);
            deathDirector.Play();
            yield return StartCoroutine(WaitForDirectorOrSkip(deathDirector));
        }

        yield return new WaitUntil(() => Input.anyKeyDown);

        Time.timeScale = 1f;
        LockPlayer(false);

        deathDirector.gameObject.SetActive(false);
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

        float duration = (float)director.duration;
        float timer = 0f;
        float lastInputTime = -10f;
        float doubleTapThreshold = 0.5f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            if (Input.anyKeyDown)
            {
                if (Time.unscaledTime - lastInputTime < doubleTapThreshold)
                {
                    // Double tap detected! Fast-forward cutscene.
                    director.time = director.duration;
                    director.Evaluate();
                    yield break;
                }
                lastInputTime = Time.unscaledTime;
            }

            yield return null;
        }
    }
}
