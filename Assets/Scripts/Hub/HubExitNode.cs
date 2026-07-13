using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Placed on Exit Altar GameObjects in the HubScene.
/// When the player logically arrives at this node, TriggerExit is called.
/// It stores the chosen memory route, increments the level, saves the run, 
/// and transitions to GameScene.
/// </summary>
public class HubExitNode : MonoBehaviour
{
    [Header("Route Configuration")]
    [SerializeField] private MemoryNodeData routeData;

    [Header("Transition")]
    [SerializeField] private float transitionDelay = 1.0f;

    private bool hasTriggered = false;

    /// <summary>
    /// Called logically by HubPlayerMovement when the player lands on this node.
    /// </summary>
    public void TriggerExit()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log($"[HubExitNode] Player logically arrived at exit altar: {(routeData != null ? routeData.nodeName : "Default")}");

        // Attempt to find the player and disable input/force fall animation
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var movement = player.GetComponent<HubPlayerMovement>();
            if (movement != null) movement.enabled = false;

            // In a purely logical setup, we might need a different way to trigger
            // the falling animation. If the animator uses "VelocityY", we can spoof it here
            // or trigger a specific animation state.
            var anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsGrounded", false);
                anim.SetFloat("VelocityY", -15f); // Spoof downward velocity for the animator
                anim.Play("Fall"); // Direct play if trigger isn't an option
            }
        }

        // Store the chosen route for GameScene to read
        if (GameSession.Instance != null)
        {
            GameSession.Instance.pendingNextNode = routeData;
            GameSession.Instance.currentRun.levelNumber++;
            GameSession.Instance.SaveCurrentRun();
        }

        StartCoroutine(TransitionToGameScene());
    }

    private IEnumerator TransitionToGameScene()
    {
        yield return new WaitForSeconds(transitionDelay);

        Debug.Log("[HubExitNode] Loading GameScene...");
        SceneManager.LoadScene("GameScene");
    }
}
