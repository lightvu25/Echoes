using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using TMPro;

public class CutsceneManager : MonoBehaviour
{
    [Header("Death Cutscene")]
    public PlayableDirector deathDirector;
    public TMP_Text shardSubTitleText;

    [Header("Goal Cutscene")]
    public PlayableDirector goalDirector;

    private void Start()
    {
        if (deathDirector != null)
        {
            deathDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            deathDirector.gameObject.SetActive(false);
        }

        if (goalDirector != null)
        {
            goalDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            goalDirector.gameObject.SetActive(false);
        }
    }

    private PlayerMovement _playerMovement;
    private PlayerAttack _playerAttack;
    private Rigidbody2D _playerRb;
    private Collider2D _playerCollider;

    private void CachePlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        _playerMovement = player.GetComponent<PlayerMovement>();
        _playerAttack = player.GetComponent<PlayerAttack>();
        _playerRb = player.GetComponent<Rigidbody2D>();
        _playerCollider = player.GetComponent<Collider2D>();
    }

    private void LockPlayer(bool locked)
    {
        if (_playerMovement == null) CachePlayerComponents();

        if (_playerMovement != null)
            _playerMovement.enabled = !locked;

        if (_playerAttack != null)
            _playerAttack.enabled = !locked;

        if (GameInput.Instance != null)
            GameInput.Instance.SetInputsEnabled(!locked);
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
            yield return new WaitForSecondsRealtime((float)deathDirector.duration);
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
            yield return new WaitForSecondsRealtime((float)goalDirector.duration);
            goalDirector.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        LockPlayer(false);
    }
}
