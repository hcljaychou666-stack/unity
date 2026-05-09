using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private bool enableDash = false;
    [SerializeField] private float dashSpeed = 21f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private int maxAirDashes = 1;
    [SerializeField] private float postDashControlLock = 0.05f;

    [Header("Invincibility")]
    [SerializeField] private bool dashInvincible = true;

    [Header("Trail")]
    [SerializeField] private bool useDashTrail = true;
    [SerializeField] private Color trailStartColor = new Color(1f, 0.9f, 0.5f, 0.7f);
    [SerializeField] private Color trailEndColor = new Color(1f, 0.5f, 0.2f, 0f);
    [SerializeField] private float trailStartWidth = 0.22f;
    [SerializeField] private float trailEndWidth = 0.02f;
    [SerializeField] private TrailRenderer dashTrail;
    [SerializeField] private float trailTime = 0.18f;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private float originalGravityScale;
    private float dashEndTime;
    private float nextDashTime;
    private int remainingAirDashes;
    private Vector2 dashVelocity;
    private bool isDashing;

    public bool IsDashing => isDashing;
    public bool IsInvincible => isDashing && dashInvincible;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        originalGravityScale = rb != null ? rb.gravityScale : 1f;
        remainingAirDashes = Mathf.Max(0, maxAirDashes);
        CacheTrail();
    }

    private void OnEnable()
    {
        remainingAirDashes = Mathf.Max(0, maxAirDashes);
    }

    private void OnDisable()
    {
        EndDash();
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            return;
        }

        if (rb == null || movement == null || rb.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        if (movement.IsGroundedCached && !isDashing)
        {
            remainingAirDashes = Mathf.Max(0, maxAirDashes);
        }

        if (isDashing)
        {
            movement.RequestMovementOverride(dashVelocity, Time.fixedDeltaTime * 2f, true);

            if (Time.time >= dashEndTime)
            {
                EndDash();
            }

            return;
        }

        if (!enableDash || Time.time < nextDashTime || !WasDashPressed())
        {
            return;
        }

        bool grounded = movement.IsGroundedCached;
        if (!grounded && remainingAirDashes <= 0)
        {
            return;
        }

        StartDash(grounded);
    }

    private void StartDash(bool grounded)
    {
        Vector2 direction = GetDashDirection();
        dashVelocity = direction * Mathf.Max(0f, dashSpeed);
        dashEndTime = Time.time + Mathf.Max(0.01f, dashDuration);
        nextDashTime = Time.time + Mathf.Max(0f, dashCooldown);
        isDashing = true;

        if (!grounded)
        {
            remainingAirDashes = Mathf.Max(0, remainingAirDashes - 1);
        }

        originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        movement.RequestMovementOverride(dashVelocity, dashDuration, true);
        SetTrailEmitting(true);
    }

    private void EndDash()
    {
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale;
        }

        if (movement != null && postDashControlLock > 0f)
        {
            movement.ApplyExternalJump(new Vector2(rb != null ? rb.velocity.x : 0f, rb != null ? rb.velocity.y : 0f), postDashControlLock);
        }

        isDashing = false;
        SetTrailEmitting(false);
    }

    private Vector2 GetDashDirection()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), GetVerticalInput());

        if (input.sqrMagnitude <= 0.01f)
        {
            input = new Vector2(Mathf.Approximately(movement.FacingDirection, 0f) ? 1f : movement.FacingDirection, 0f);
        }

        return input.normalized;
    }

    private static float GetVerticalInput()
    {
        float axis = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(axis) > 0.01f)
        {
            return axis;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            return 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            return -1f;
        }

        return 0f;
    }

    private static bool WasDashPressed()
    {
        return Input.GetKeyDown(KeyCode.LeftShift)
            || Input.GetKeyDown(KeyCode.RightShift)
            || Input.GetMouseButtonDown(1);
    }

    private void CacheTrail()
    {
        if (dashTrail == null)
        {
            dashTrail = GetComponent<TrailRenderer>();
        }

        if (dashTrail == null)
        {
            return;
        }

        dashTrail.time = Mathf.Max(0.01f, trailTime);
        ApplyTrailStyle();
        dashTrail.emitting = false;
    }

    private void SetTrailEmitting(bool emitting)
    {
        if (!useDashTrail)
        {
            emitting = false;
        }

        if (dashTrail == null && useDashTrail)
        {
            dashTrail = gameObject.AddComponent<TrailRenderer>();
            dashTrail.time = Mathf.Max(0.01f, trailTime);
            ApplyTrailStyle();
        }

        if (dashTrail != null)
        {
            dashTrail.emitting = emitting;
        }
    }

    private void ApplyTrailStyle()
    {
        if (dashTrail == null)
        {
            return;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(trailStartColor, 0f), new GradientColorKey(trailEndColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(trailStartColor.a, 0f), new GradientAlphaKey(trailEndColor.a, 1f) }
        );

        dashTrail.colorGradient = gradient;
        dashTrail.startWidth = trailStartWidth;
        dashTrail.endWidth = trailEndWidth;
    }
}
