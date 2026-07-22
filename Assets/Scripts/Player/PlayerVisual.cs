using System;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [Header("Particles")]
    [SerializeField] private ParticleSystem particleWalkingPrefab;
    [SerializeField] private ParticleSystem particleJumpPrefab;
    [SerializeField] private ParticleSystem particleLandPrefab;
    [SerializeField] private ParticleSystem particleDiePrefab;

    [Header("Components")]
    [SerializeField] private Animator _animator;

    private PlayerMovement playerMovement;
    private PlayerInteract playerInteract;
    private PlayerAttack playerAttack;
    private PlayerCombat playerCombat;
    private CrimsonAmber crimsonAmber;
    private PlayerTool playerTool;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInteract = GetComponent<PlayerInteract>();
        playerAttack = GetComponent<PlayerAttack>();
        playerCombat = GetComponent<PlayerCombat>();
        crimsonAmber = GetComponent<CrimsonAmber>();
        playerTool = GetComponent<PlayerTool>();
    }

    private void Start()
    {
        playerMovement.OnJump += HandleJump;
        playerMovement.OnLand += HandleLand;
        playerMovement.OnGrab += HandleLedgeGrab;
        playerMovement.OnGetup += HandleLedgeClimb;
        playerMovement.OnFall += HandleFall;
        playerMovement.OnDash += HandleDash;
        playerInteract.OnDead += HandleDead;
        playerAttack.OnAttackStarted += HandleAttack;
        if (playerCombat != null) playerCombat.OnDamageReceived += HandleDamage;
        if (crimsonAmber != null) crimsonAmber.OnConsume += HandleConsume;
        if (playerTool != null) playerTool.OnConsume += HandleConsume;
    }

    private void Update()
    {
        if (playerMovement == null) return;
        
        _animator.SetBool("isGrounded", playerMovement.isGrounded);
        _animator.SetBool("isRunning", playerMovement.isRunning);
        _animator.SetFloat("VelocityY", playerMovement.rb.linearVelocity.y);
        _animator.SetBool("isWallSliding", playerMovement.isSliding);
        _animator.SetBool("isClimbing", playerMovement.isClimbing);
        _animator.SetBool("isPlunging", playerMovement.isPlunging);
        _animator.SetBool("isDashing", playerMovement.isDashing);

        if (particleWalkingPrefab != null)
        {
            if (playerMovement.isGrounded && playerMovement.isRunning)
            {
                if (!particleWalkingPrefab.isPlaying) particleWalkingPrefab.Play();
            }
            else
            {
                if (particleWalkingPrefab.isPlaying) particleWalkingPrefab.Stop();
            }
        }
    }


    private void HandleJump(object sender, EventArgs e)
    {
        _animator.Play("Jump");
        
        if (particleJumpPrefab != null) ObjectPoolManager.SpawnObject(particleJumpPrefab.gameObject, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
    }

    private void HandleLand(object sender, EventArgs e)
    {
        if (particleLandPrefab != null) 
            ObjectPoolManager.SpawnObject(particleLandPrefab.gameObject, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
    }

    private void HandleFall(object sender, EventArgs e)
    {
        if (playerAttack != null && playerAttack.IsAttacking) return;
        _animator.Play("Fall");
    }

    private void HandleDash(object sender, EventArgs e)
    {
        _animator.Play("Dash");
    }

    private void HandleConsume()
    {
        _animator.Play("Consume");
    }

    private void HandleLedgeGrab(object sender, EventArgs e)
    {
        _animator.Play("Ledge grab"); 
    }

    private void HandleLedgeClimb(object sender, EventArgs e)
    {
        _animator.Play("Ledge Climb");
    }

    private void HandleAttack(object sender, EventArgs e)
    {
        if (e is PlayerAttack.AttackEventArgs attackArgs)
        {
            if (!string.IsNullOrEmpty(attackArgs.animationName))
            {
                _animator.Play(attackArgs.animationName);
            }
        }
    }

    private void HandleDead(object sender, EventArgs e)
    {
        _animator.Play("Death");
        if (particleDiePrefab != null) ObjectPoolManager.SpawnObject(particleDiePrefab.gameObject, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
    }

    private void HandleDamage(object sender, PlayerCombat.DamageReceivedArgs e)
    {
        if (e.damage > 0)
        {
            _animator.Play("Hurt");
        }
    }

    private void OnDestroy()
    {
        if (playerMovement != null)
        {
            playerMovement.OnJump -= HandleJump;
            playerMovement.OnLand -= HandleLand;
            playerMovement.OnGrab -= HandleLedgeGrab;
            playerMovement.OnGetup -= HandleLedgeClimb;
            playerMovement.OnFall -= HandleFall;
            playerMovement.OnDash -= HandleDash;
        }
        if (playerInteract != null)
        {
            playerInteract.OnDead -= HandleDead;
        }
        if (playerAttack != null)
        {
            playerAttack.OnAttackStarted -= HandleAttack;
        }
        if (playerCombat != null)
        {
            playerCombat.OnDamageReceived -= HandleDamage;
        }
        if (crimsonAmber != null)
        {
            crimsonAmber.OnConsume -= HandleConsume;
        }
        if (playerTool != null)
        {
            playerTool.OnConsume -= HandleConsume;
        }
    }
}