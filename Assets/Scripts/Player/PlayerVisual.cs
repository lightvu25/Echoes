using System;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleWalkingPrefab;
    [SerializeField] private ParticleSystem particleJumpPrefab;
    [SerializeField] private ParticleSystem particleLandPrefab;
    [SerializeField] private ParticleSystem particleDiePrefab;

    [SerializeField] private Animator _animator;

    private PlayerMovement playerMovement;
    private PlayerInteract playerInteract;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInteract = GetComponent<PlayerInteract>();
    }

    private void Start()
    {
        playerMovement.OnIdle += HandleIdle;
        playerMovement.OnJump += HandleJump;
        playerMovement.OnLand += HandleLand;
        playerMovement.OnRun += HandleRun;
        playerInteract.OnDead += HandleDead;
    }

    private void HandleIdle(object sender, EventArgs e)
    {
        _animator.SetBool("IsRunning", false);
        _animator.SetBool("IsJumping", false);
        _animator.SetBool("IsFalling", false);
        _animator.SetBool("IsDashing", false);
    }

    private void HandleJump(object sender, EventArgs e)
    {
        if (particleJumpPrefab != null)
        {
            Instantiate(particleJumpPrefab, transform.position, Quaternion.identity);
        }

        if (particleWalkingPrefab != null)
            particleWalkingPrefab.Stop();
    }

    private void HandleLand(object sender, EventArgs e)
    {
        if (particleLandPrefab != null)
        {
            Instantiate(particleLandPrefab, transform.position, Quaternion.identity);
        }

        if (particleWalkingPrefab != null)
            particleWalkingPrefab.Play();
    }

    private void HandleRun(object sender, EventArgs e)
    {
        if (particleWalkingPrefab != null)
            particleWalkingPrefab.Play();
    }

    private void HandleDead(object sender, EventArgs e)
    {
        // animator.SetTrigger("Die");
    }
}
