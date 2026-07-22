using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Placed on Exit Altar GameObjects in the MindScene.
/// When the player logically arrives at this node, TriggerExit is called.
/// It stores the accumulated Magic Toxicity and Relic Bonus, 
/// increments the level, and transitions to GameScene.
/// </summary>
public class MindExitNode : MonoBehaviour
{
    [Header("Route Configuration")]
    [SerializeField] private MemoryNodeData routeData;

    [Header("Transition")]
    [SerializeField] private float transitionDelay = 1.0f;

    private bool hasTriggered = false;

    public void Interact()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log($"[MindExitNode] Player interacting with exit altar: {(routeData != null ? routeData.nodeName : "Default")}");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<MindPlayerMovement>();
            if (movement != null) movement.enabled = false;

            var anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsGrounded", false);
                anim.SetFloat("VelocityY", -15f); 
                anim.Play("Fall"); 
            }
        }

        if (GameSession.Instance != null && GameSession.Instance.currentRun != null)
        {
            GameSession.Instance.pendingNextNode = routeData;
            GameSession.Instance.currentRun.levelNumber++;

            GameSession.Instance.currentRun.currentLevelTime = 0f; // Reset for the next level
            GameSession.Instance.currentRun.currentLevelNoHitKills = 0;

            GameSession.Instance.SaveCurrentRun();
        }

        StartCoroutine(TransitionToGameScene());
    }

    private IEnumerator TransitionToGameScene()
    {
        var cutsceneManager = FindFirstObjectByType<CutsceneManager>();
        if (cutsceneManager != null && cutsceneManager.goalDirector != null)
        {
            cutsceneManager.goalDirector.gameObject.SetActive(true);
            cutsceneManager.goalDirector.Play();
            yield return new WaitForSeconds((float)cutsceneManager.goalDirector.duration);
        }
        else
        {
            yield return new WaitForSeconds(transitionDelay);
        }

        Debug.Log("[MindExitNode] Loading GameScene...");
        SceneManager.LoadScene("GameScene");
    }
}
