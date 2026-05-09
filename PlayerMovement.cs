using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator anim;
    private BoxCollider2D coll;

    [Header("Move")]
    [SerializeField] private LayerMask jumpableGround;
    [SerializeField, FormerlySerializedAs("movespeed")] private float moveSpeed = 7f;
    [SerializeField, FormerlySerializedAs("jumpforce")] private float jumpForce = 7f;
    [SerializeField] private AudioSource jumpSoundEffect;

    [Header("Jump Assist")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("Variable Jump")]
    [SerializeField] private bool enableVariableJumpHeight = false;
    [SerializeField, Range(0.05f, 1f)] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float minimumJumpCutVelocity = 0.1f;

    [Header("Shoot")]
    [SerializeField] private AudioSource shootSoundEffect;
    [SerializeField] private AudioClip shootSoundClip;
    [SerializeField] private float shootSoundVolume = 0.55f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float projectileSpeed = 14f;
    [SerializeField] private float projectileLifetime = 1.1f;
    [SerializeField] private float shootCooldown = 0.35f;
    [SerializeField] private Vector2 shootOffset = new Vector2(0.24f, 0.02f);

    [Header("Movement Effects")]
    [SerializeField] private bool enableMovementEffects = false;
    [SerializeField] private PixelEffectSpawner effectSpawner;
    [SerializeField] private ParticleSystem runDustParticles;
    [SerializeField] private ParticleSystem jumpParticles;
    [SerializeField] private ParticleSystem landingParticles;
    [SerializeField] private ParticleSystem shootMuzzleFlash;
    [SerializeField] private float runDustInterval = 0.16f;
    [SerializeField] private Vector2 groundEffectOffset = new Vector2(0f, -0.16f);

    [Header("Landing Shake")]
    [SerializeField] private bool enableLandingShake = false;
    [SerializeField] private CameraFollow landingShakeTarget;
    [SerializeField] private float lightLandingSpeedThreshold = 2.5f;
    [SerializeField] private float heavyLandingSpeedThreshold = 8f;
    [SerializeField] private float lightLandingShakeIntensity = 0.035f;
    [SerializeField] private float lightLandingShakeDuration = 0.06f;
    [SerializeField] private float heavyLandingShakeIntensity = 0.08f;
    [SerializeField] private float heavyLandingShakeDuration = 0.1f;

    private float horizontalInput;
    private float nextShootTime;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private AudioClip generatedShootClip;
    private bool isGroundedCached;
    private bool wasGrounded;
    private float facingDirection = 1f;
    private float movementOverrideUntil;
    private Vector2 movementOverrideVelocity;
    private bool lockAnimationDuringOverride;
    private float movementControlLockUntil;
    private float fallSpeedLimitUntil;
    private float requestedMaxFallSpeed;
    private float nextRunDustTime;
    private float lastAirborneFallSpeed;

    private enum MovementState { idle, running, jumping, falling }

    public float HorizontalInput => horizontalInput;
    public float FacingDirection => facingDirection;
    public bool IsGroundedCached => isGroundedCached;
    public bool IsMovementOverrideActive => Time.time < movementOverrideUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        coll = GetComponent<BoxCollider2D>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void OnEnable()
    {
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        movementOverrideUntil = 0f;
        movementControlLockUntil = 0f;
        fallSpeedLimitUntil = 0f;
        lastAirborneFallSpeed = 0f;
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            return;
        }

        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
        UpdateJumpAssistTimers();
        TryCutJumpShort();

        if (CanShoot() && (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.F)))
        {
            Shoot();
        }

        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        if (IsMovementOverrideActive)
        {
            rb.velocity = movementOverrideVelocity;
            return;
        }

        float targetHorizontalVelocity = Time.time < movementControlLockUntil
            ? rb.velocity.x
            : horizontalInput * moveSpeed;

        rb.velocity = new Vector2(targetHorizontalVelocity, rb.velocity.y);

        if (CanBufferedJump())
        {
            Jump();
        }

        ApplyFallSpeedLimit();
        TryPlayRunDust();
    }

    private void UpdateJumpAssistTimers()
    {
        wasGrounded = isGroundedCached;
        isGroundedCached = IsGrounded();
        TrackAirborneFallSpeed();
        coyoteCounter = isGroundedCached ? coyoteTime : coyoteCounter - Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (!wasGrounded && isGroundedCached)
        {
            PlayEffect(landingParticles);
            TryTriggerLandingShake(lastAirborneFallSpeed);
            lastAirborneFallSpeed = 0f;
        }
    }

    private void TrackAirborneFallSpeed()
    {
        if (rb == null)
        {
            lastAirborneFallSpeed = 0f;
            return;
        }

        if (isGroundedCached)
        {
            if (wasGrounded)
            {
                lastAirborneFallSpeed = 0f;
            }

            return;
        }

        lastAirborneFallSpeed = Mathf.Max(lastAirborneFallSpeed, Mathf.Max(0f, -rb.velocity.y));
    }

    private bool CanBufferedJump()
    {
        return jumpBufferCounter > 0f && coyoteCounter > 0f && !IsMovementOverrideActive;
    }

    private void Jump()
    {
        if (jumpSoundEffect != null)
        {
            jumpSoundEffect.Play();
        }

        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        PlayEffect(jumpParticles);
    }

    public void RequestMovementOverride(Vector2 velocity, float duration, bool lockAnimation)
    {
        movementOverrideVelocity = velocity;
        movementOverrideUntil = Mathf.Max(movementOverrideUntil, Time.time + Mathf.Max(0f, duration));
        lockAnimationDuringOverride = lockAnimation;
    }

    public void ApplyExternalJump(Vector2 velocity, float controlLockDuration)
    {
        if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        rb.velocity = velocity;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        movementControlLockUntil = Mathf.Max(movementControlLockUntil, Time.time + Mathf.Max(0f, controlLockDuration));
        PlayEffect(jumpParticles);
    }

    public void RequestFallSpeedLimit(float maxFallSpeed, float duration)
    {
        requestedMaxFallSpeed = Mathf.Max(0f, maxFallSpeed);
        fallSpeedLimitUntil = Mathf.Max(fallSpeedLimitUntil, Time.time + Mathf.Max(0f, duration));
    }

    private void TryCutJumpShort()
    {
        if (!enableVariableJumpHeight
            || IsMovementOverrideActive
            || !Input.GetButtonUp("Jump")
            || rb.velocity.y <= minimumJumpCutVelocity)
        {
            return;
        }

        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * Mathf.Clamp01(jumpCutMultiplier));
    }

    private void ApplyFallSpeedLimit()
    {
        if (Time.time >= fallSpeedLimitUntil || requestedMaxFallSpeed <= 0f || rb.velocity.y >= -requestedMaxFallSpeed)
        {
            return;
        }

        rb.velocity = new Vector2(rb.velocity.x, -requestedMaxFallSpeed);
    }

    private bool CanShoot()
    {
        return Time.time >= nextShootTime;
    }

    private void Shoot()
    {
        nextShootTime = Time.time + shootCooldown;

        float direction = sprite != null && sprite.flipX ? -1f : 1f;
        GameObject projectileObject = CreateProjectileObject();

        if (projectileObject == null)
        {
            return;
        }

        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<Projectile>();
        }

        SpriteRenderer projectileSprite = projectileObject.GetComponent<SpriteRenderer>();

        if (projectileSprite != null)
        {
            projectileSprite.flipX = direction < 0f;
            projectileSprite.sortingLayerID = sprite != null ? sprite.sortingLayerID : projectileSprite.sortingLayerID;
            projectileSprite.sortingOrder = sprite != null ? sprite.sortingOrder + 1 : projectileSprite.sortingOrder;
            projectileObject.transform.localScale = GetProjectileScale(projectileSprite.sprite);
        }

        projectileObject.transform.position = GetShootSpawnPosition(direction, projectileSprite);
        projectileObject.transform.rotation = Quaternion.identity;

        projectile.Initialize(direction, projectileSpeed, projectileLifetime, gameObject, projectileDamage);
        PlayShootSound();
        PlayEffect(shootMuzzleFlash, GetShootSpawnPosition(direction, projectileSprite));
    }

    private void PlayShootSound()
    {
        if (shootSoundEffect == null)
        {
            shootSoundEffect = gameObject.AddComponent<AudioSource>();
            shootSoundEffect.playOnAwake = false;
            shootSoundEffect.spatialBlend = 0f;
        }

        AudioClip clip = shootSoundClip != null ? shootSoundClip : GetGeneratedShootClip();
        if (clip != null)
        {
            shootSoundEffect.PlayOneShot(clip, Mathf.Clamp01(shootSoundVolume));
        }
    }

    private AudioClip GetGeneratedShootClip()
    {
        if (generatedShootClip != null)
        {
            return generatedShootClip;
        }

        const int sampleRate = 22050;
        const float duration = 0.075f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float normalized = i / (float)sampleCount;
            float frequency = Mathf.Lerp(880f, 360f, normalized);
            float envelope = Mathf.Exp(-normalized * 7f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.7f;
        }

        generatedShootClip = AudioClip.Create("GeneratedShootBlip", sampleCount, 1, sampleRate, false);
        generatedShootClip.SetData(samples, 0);
        return generatedShootClip;
    }

    private GameObject CreateProjectileObject()
    {
        return Projectile.CreateVisualProjectile();
    }

    private Vector3 GetShootSpawnPosition(float direction, SpriteRenderer projectileSprite)
    {
        Vector3 basePosition = coll != null ? coll.bounds.center : transform.position;
        float projectileHalfWidth = projectileSprite != null ? projectileSprite.bounds.extents.x : 0.2f;
        float horizontalOffset = (coll != null ? coll.bounds.extents.x : 0.25f) + projectileHalfWidth + Mathf.Abs(shootOffset.x);
        float verticalOffset = shootOffset.y + (coll != null ? coll.bounds.extents.y * 0.15f : 0f);
        return basePosition + new Vector3(horizontalOffset * direction, verticalOffset, 0f);
    }

    private Vector3 GetProjectileScale(Sprite projectileSprite)
    {
        float playerHeight = coll != null ? coll.bounds.size.y : 1.5f;
        float desiredHeight = Mathf.Max(0.5f, playerHeight * 0.33f);
        float spriteHeight = projectileSprite != null ? projectileSprite.bounds.size.y : 0.12f;
        float uniformScale = Mathf.Max(1f, desiredHeight / Mathf.Max(0.01f, spriteHeight));
        return new Vector3(uniformScale, uniformScale, 1f);
    }

    private void UpdateAnimationState()
    {
        if (anim == null || sprite == null || rb == null)
        {
            return;
        }

        MovementState state;

        if (horizontalInput > 0f)
        {
            state = MovementState.running;
            if (!lockAnimationDuringOverride || !IsMovementOverrideActive)
            {
                sprite.flipX = false;
                facingDirection = 1f;
            }
        }
        else if (horizontalInput < 0f)
        {
            state = MovementState.running;
            if (!lockAnimationDuringOverride || !IsMovementOverrideActive)
            {
                sprite.flipX = true;
                facingDirection = -1f;
            }
        }
        else
        {
            state = MovementState.idle;
        }

        if (rb.velocity.y > .1f)
        {
            state = MovementState.jumping;
        }
        else if (rb.velocity.y < -.1f)
        {
            state = MovementState.falling;
        }

        anim.SetInteger("state", (int)state);
    }

    private void TryPlayRunDust()
    {
        if (!enableMovementEffects
            || !isGroundedCached
            || Mathf.Abs(horizontalInput) <= 0.1f
            || Time.time < nextRunDustTime)
        {
            return;
        }

        nextRunDustTime = Time.time + Mathf.Max(0.02f, runDustInterval);
        PlayEffect(runDustParticles);
    }

    private void PlayEffect(ParticleSystem particles)
    {
        Vector3 position = transform.position + (Vector3)groundEffectOffset;
        PlayEffect(particles, position);
    }

    private void PlayEffect(ParticleSystem particles, Vector3 position)
    {
        if (!enableMovementEffects || particles == null)
        {
            return;
        }

        PixelEffectSpawner spawner = effectSpawner != null ? effectSpawner : GetComponent<PixelEffectSpawner>();
        if (spawner == null)
        {
            spawner = FindObjectOfType<PixelEffectSpawner>();
        }

        if (spawner != null)
        {
            spawner.Play(particles, position, Quaternion.identity);
        }
    }

    private void TryTriggerLandingShake(float fallSpeed)
    {
        if (!enableLandingShake || fallSpeed < Mathf.Max(0f, lightLandingSpeedThreshold))
        {
            return;
        }

        float heavyThreshold = Mathf.Max(lightLandingSpeedThreshold, heavyLandingSpeedThreshold);
        bool heavyLanding = fallSpeed >= heavyThreshold;
        float intensity = heavyLanding ? heavyLandingShakeIntensity : lightLandingShakeIntensity;
        float duration = heavyLanding ? heavyLandingShakeDuration : lightLandingShakeDuration;

        if (intensity <= 0f || duration <= 0f)
        {
            return;
        }

        CameraFollow shakeTarget = landingShakeTarget != null ? landingShakeTarget : FindObjectOfType<CameraFollow>();
        if (shakeTarget != null)
        {
            shakeTarget.TriggerShake((Vector2)transform.position, intensity, duration);
        }
    }

    private bool IsGrounded()
    {
        if (coll == null)
        {
            return false;
        }

        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, groundCheckDistance, jumpableGround);
    }
}
