using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerWallInteraction : MonoBehaviour
{
    [Header("Wall Slide")]
    [SerializeField] private bool enableWallSlide = false;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.18f;
    [SerializeField] private float wallSlideSpeed = 1.5f;

    [Header("Wall Jump")]
    [SerializeField] private Vector2 wallJumpForce = new Vector2(7.5f, 7f);
    [SerializeField] private float wallJumpControlLockTime = 0.16f;

    [Header("Debug")]
    [SerializeField] private bool drawWallCheckGizmos = true;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private Collider2D bodyCollider;
    private bool isTouchingWall;
    private float wallDirection;

    public bool IsTouchingWall => isTouchingWall;
    public float WallDirection => wallDirection;

    private bool CanWallInteract => enableWallSlide && rb != null && movement != null && !movement.IsGroundedCached;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        bodyCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            return;
        }

        RefreshWallContact();

        if (!CanWallInteract || !isTouchingWall || !Input.GetButtonDown("Jump"))
        {
            return;
        }

        Vector2 jumpVelocity = new Vector2(-wallDirection * wallJumpForce.x, wallJumpForce.y);
        movement.ApplyExternalJump(jumpVelocity, wallJumpControlLockTime);
    }

    private void FixedUpdate()
    {
        if (!CanWallInteract || !isTouchingWall || rb.velocity.y >= -wallSlideSpeed)
        {
            return;
        }

        movement.RequestFallSpeedLimit(wallSlideSpeed, Time.fixedDeltaTime * 2f);
    }

    private void RefreshWallContact()
    {
        isTouchingWall = false;
        wallDirection = 0f;

        if (!enableWallSlide || bodyCollider == null || wallLayer.value == 0)
        {
            return;
        }

        Bounds bounds = bodyCollider.bounds;
        Vector2 castSize = new Vector2(Mathf.Max(0.03f, bounds.size.x * 0.2f), bounds.size.y * 0.82f);
        Vector2 center = bounds.center;

        if (Physics2D.BoxCast(center, castSize, 0f, Vector2.right, wallCheckDistance, wallLayer))
        {
            isTouchingWall = true;
            wallDirection = 1f;
            return;
        }

        if (Physics2D.BoxCast(center, castSize, 0f, Vector2.left, wallCheckDistance, wallLayer))
        {
            isTouchingWall = true;
            wallDirection = -1f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawWallCheckGizmos)
        {
            return;
        }

        Collider2D selectedCollider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        if (selectedCollider == null)
        {
            return;
        }

        Bounds bounds = selectedCollider.bounds;
        Vector3 center = bounds.center;
        float distance = Mathf.Max(0f, wallCheckDistance);

        Gizmos.color = enableWallSlide ? Color.cyan : new Color(0.4f, 0.4f, 0.4f, 0.65f);
        Gizmos.DrawLine(center, center + Vector3.right * distance);
        Gizmos.DrawLine(center, center + Vector3.left * distance);
    }
}
