using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerVisual : MonoBehaviour
{
    [Header("Particles")]
    [SerializeField] private ParticleSystem particleWalkingPrefab;
    [SerializeField] private ParticleSystem particleJumpPrefab;
    [SerializeField] private ParticleSystem particleLandPrefab;
    [SerializeField] private ParticleSystem particleDiePrefab;

    [Header("Dash Feedback")]
    [SerializeField] private bool createDashVFXAtRuntime = true;
    [SerializeField] private TrailRenderer dashTrail;
    [FormerlySerializedAs("dashParticles")]
    [SerializeField] private ParticleSystem dashParticlePrefab;
    [SerializeField] private Vector3 dashParticleLocalOffset = new Vector3(0f, 0.55f, 0f);
    [SerializeField] private Color dashColor = new Color(0.2f, 0.85f, 1f, 1f);
    [SerializeField, Min(1f)] private float dashGlowIntensity = 2.5f;
    [SerializeField, Min(0.01f)] private float dashTrailTime = 0.16f;
    [SerializeField, Min(0.01f)] private float dashTrailWidth = 0.24f;
    [Tooltip("World-space distance between dash afterimages. Smaller values create more afterimages.")]
    [SerializeField, Min(0.05f)] private float dashParticleSpacing = 0.28f;
    [SerializeField, Min(0.01f)] private float dashParticleLifetime = 0.22f;

    [Header("Components")]
    [SerializeField] private Animator _animator;

    private PlayerMovement playerMovement;
    private PlayerInteract playerInteract;
    private PlayerAttack playerAttack;
    private PlayerCombat playerCombat;
    private CrimsonAmber crimsonAmber;
    private PlayerTool playerTool;
    private Coroutine dashFeedbackRoutine;
    private Material runtimeDashMaterial;
    private ParticleSystem dashParticles;
    private Vector3 dashTrailBaseLocalScale = Vector3.one;
    private bool dashTrailScaleCached;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInteract = GetComponent<PlayerInteract>();
        playerAttack = GetComponent<PlayerAttack>();
        playerCombat = GetComponent<PlayerCombat>();
        crimsonAmber = GetComponent<CrimsonAmber>();
        playerTool = GetComponent<PlayerTool>();
        EnsureDashFeedback();
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

        Vector2 direction = playerMovement != null
            ? playerMovement.LastDashDirection
            : (transform.localScale.x >= 0f ? Vector2.right : Vector2.left);
        GameFeelManager.Instance?.ProcessDash(transform.position, direction);

        if (dashFeedbackRoutine != null) StopCoroutine(dashFeedbackRoutine);
        dashFeedbackRoutine = StartCoroutine(PlayDashFeedback(direction));
    }

    private void EnsureDashFeedback()
    {
        if (dashTrail == null && createDashVFXAtRuntime)
        {
            EnsureRuntimeDashMaterial();
            GameObject trailObject = new GameObject("Dash Streak");
            trailObject.layer = gameObject.layer;
            trailObject.transform.SetParent(transform, false);
            dashTrail = trailObject.AddComponent<TrailRenderer>();
            dashTrail.emitting = false;
            dashTrail.time = dashTrailTime;
            dashTrail.minVertexDistance = 0.035f;
            dashTrail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.65f, 0.65f),
                new Keyframe(1f, 0f));
            dashTrail.widthMultiplier = dashTrailWidth;
            dashTrail.alignment = LineAlignment.TransformZ;
            dashTrail.textureMode = LineTextureMode.Stretch;
            dashTrail.numCapVertices = 2;
            dashTrail.sortingLayerName = "VFX";
            dashTrail.sortingOrder = 75;
            if (runtimeDashMaterial != null) dashTrail.sharedMaterial = runtimeDashMaterial;
        }

        if (dashParticles == null)
        {
            if (dashParticlePrefab != null)
                CreateDashParticleFromPrefab();
            else if (createDashVFXAtRuntime)
                CreateFallbackDashParticles();
        }

        ConfigureDashParticleInstance();
        CacheDashTrailScale();
        ApplyDashColor();
    }

    private void CacheDashTrailScale()
    {
        if (dashTrail == null || dashTrailScaleCached) return;

        dashTrailBaseLocalScale = dashTrail.transform.localScale;
        dashTrailScaleCached = true;
    }

    private void CreateDashParticleFromPrefab()
    {
        dashParticles = Instantiate(dashParticlePrefab, transform, false);
        dashParticles.name = "Dash Particles";
        dashParticles.gameObject.layer = gameObject.layer;
        dashParticles.transform.localPosition = dashParticleLocalOffset;
        dashParticles.transform.localRotation = Quaternion.identity;
        dashParticles.transform.localScale = Vector3.one;
    }

    private void CreateFallbackDashParticles()
    {
        EnsureRuntimeDashMaterial();

        GameObject particleObject = new GameObject("Dash Particles");
        particleObject.layer = gameObject.layer;
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.localPosition = dashParticleLocalOffset;
        dashParticles = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = dashParticles.main;
        main.duration = 0.25f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.13f);
        main.maxParticles = 80;

        ParticleSystem.ShapeModule shape = dashParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;

        ParticleSystemRenderer particleRenderer = dashParticles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortingLayerName = "VFX";
        particleRenderer.sortingOrder = 74;
        if (runtimeDashMaterial != null) particleRenderer.sharedMaterial = runtimeDashMaterial;
    }

    private void ConfigureDashParticleInstance()
    {
        if (dashParticles == null) return;

        ParticleSystem.MainModule main = dashParticles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.stopAction = ParticleSystemStopAction.None;
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            dashParticleLifetime * 0.65f,
            dashParticleLifetime);
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = dashParticles.emission;
        // Dash afterimages are emitted manually by travelled distance. Time-based
        // emission produces uneven gaps whenever the frame rate or dash speed changes.
        emission.enabled = false;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;

        ParticleSystem.ShapeModule shape = dashParticles.shape;
        shape.enabled = false;

        ParticleSystem.VelocityOverLifetimeModule velocity = dashParticles.velocityOverLifetime;
        velocity.enabled = false;

        dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void EnsureRuntimeDashMaterial()
    {
        if (runtimeDashMaterial != null) return;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) return;

        runtimeDashMaterial = new Material(shader)
        {
            name = "Runtime Dash VFX Material",
            hideFlags = HideFlags.DontSave
        };
    }

    private void ApplyDashColor()
    {
        Color bright = dashColor * dashGlowIntensity;
        bright.a = dashColor.a;

        if (dashTrail != null)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(bright, 0f),
                    new GradientColorKey(dashColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(dashColor.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            dashTrail.colorGradient = gradient;
            dashTrail.time = dashTrailTime;
            dashTrail.widthMultiplier = dashTrailWidth;
        }

        if (dashParticles != null)
        {
            ParticleSystem.MainModule main = dashParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(bright, dashColor);
        }
    }

    private IEnumerator PlayDashFeedback(Vector2 direction)
    {
        EnsureDashFeedback();
        Vector2 normalizedDirection = direction.sqrMagnitude > 0.001f
            ? direction.normalized
            : Vector2.right;

        if (dashTrail != null)
        {
            StabilizeDashTrailScale();
            dashTrail.Clear();
            dashTrail.emitting = true;
        }

        if (dashParticles != null)
        {
            ApplyDashParticleFacing(normalizedDirection);
            dashParticles.Clear(true);
            dashParticles.Play(true);
        }

        Vector3 previousParticlePosition = GetDashParticleWorldPosition();
        float distanceSinceLastParticle = 0f;
        EmitDashParticle(previousParticlePosition);

        do
        {
            yield return null;
            StabilizeDashTrailScale();

            Vector3 currentParticlePosition = GetDashParticleWorldPosition();
            EmitDashParticlesAlongSegment(
                previousParticlePosition,
                currentParticlePosition,
                ref distanceSinceLastParticle);
            previousParticlePosition = currentParticlePosition;
        }
        while (playerMovement != null && playerMovement.isDashing);

        StopDashFeedback();
        dashFeedbackRoutine = null;
    }

    private Vector3 GetDashParticleWorldPosition()
    {
        return dashParticles != null
            ? dashParticles.transform.position
            : transform.TransformPoint(dashParticleLocalOffset);
    }

    private void EmitDashParticlesAlongSegment(
        Vector3 segmentStart,
        Vector3 segmentEnd,
        ref float distanceSinceLastParticle)
    {
        if (dashParticles == null) return;

        Vector3 segment = segmentEnd - segmentStart;
        float segmentLength = segment.magnitude;
        if (segmentLength <= Mathf.Epsilon) return;

        float spacing = Mathf.Max(0.05f, dashParticleSpacing);
        Vector3 direction = segment / segmentLength;
        float distanceAlongSegment = 0f;

        while (distanceSinceLastParticle + (segmentLength - distanceAlongSegment) >= spacing)
        {
            float distanceToNextParticle = spacing - distanceSinceLastParticle;
            distanceAlongSegment += distanceToNextParticle;
            EmitDashParticle(segmentStart + direction * distanceAlongSegment);
            distanceSinceLastParticle = 0f;
        }

        distanceSinceLastParticle += segmentLength - distanceAlongSegment;
    }

    private void EmitDashParticle(Vector3 worldPosition)
    {
        if (dashParticles == null) return;

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = worldPosition,
            applyShapeToPosition = false
        };
        dashParticles.Emit(emitParams, 1);
    }

    private void ApplyDashParticleFacing(Vector2 dashDirection)
    {
        if (dashParticles == null) return;

        bool facingLeft = Mathf.Abs(dashDirection.x) > 0.01f
            ? dashDirection.x < 0f
            : playerMovement != null && !playerMovement.isFacingRight;

        ParticleSystemRenderer particleRenderer = dashParticles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer == null) return;

        Vector3 flip = particleRenderer.flip;
        flip.x = facingLeft ? 1f : 0f;
        particleRenderer.flip = flip;
    }

    private void StabilizeDashTrailScale()
    {
        if (dashTrail == null) return;

        CacheDashTrailScale();
        Transform trailTransform = dashTrail.transform;
        Transform trailParent = trailTransform.parent;
        if (trailParent == null || !trailTransform.IsChildOf(transform)) return;

        Vector3 stableScale = dashTrailBaseLocalScale;
        float scaleMagnitude = Mathf.Abs(stableScale.x);
        if (scaleMagnitude <= Mathf.Epsilon) return;

        stableScale.x = trailParent.lossyScale.x < 0f
            ? -scaleMagnitude
            : scaleMagnitude;
        trailTransform.localScale = stableScale;
    }

    private void StopDashFeedback()
    {
        if (dashTrail != null) dashTrail.emitting = false;
        if (dashParticles != null)
            dashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void OnDisable()
    {
        if (dashFeedbackRoutine != null)
        {
            StopCoroutine(dashFeedbackRoutine);
            dashFeedbackRoutine = null;
        }
        StopDashFeedback();
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
        _animator.Play("Die");
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
        if (dashFeedbackRoutine != null)
        {
            StopCoroutine(dashFeedbackRoutine);
            dashFeedbackRoutine = null;
        }
        StopDashFeedback();

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

        if (runtimeDashMaterial != null)
        {
            Destroy(runtimeDashMaterial);
            runtimeDashMaterial = null;
        }
    }
}
