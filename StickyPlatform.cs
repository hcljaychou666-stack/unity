using UnityEngine;

public class StickyPlatform : MonoBehaviour
{
    [SerializeField] private float topContactTolerance = 0.24f;
    [SerializeField] private float horizontalInset = 0.02f;

    private Collider2D platformCollider;
    private Collider2D carriedCollider;
    private Rigidbody2D carriedBody;
    private Transform carriedTransform;
    private Vector3 lastPlatformPosition;

    private void Awake()
    {
        CachePlatformCollider();
        lastPlatformPosition = transform.position;
    }

    private void OnEnable()
    {
        lastPlatformPosition = transform.position;
    }

    private void LateUpdate()
    {
        Vector3 platformDelta = transform.position - lastPlatformPosition;

        if (carriedTransform != null)
        {
            if (!CanCarry(carriedCollider) || !IsStandingOnPlatform(carriedCollider))
            {
                ClearCarriedPlayer();
            }
            else if (platformDelta.sqrMagnitude > 0f)
            {
                CarryPlayer(platformDelta);
            }
        }

        lastPlatformPosition = transform.position;
    }

    private bool CanCarry(Collider2D collision)
    {
        return isActiveAndEnabled
            && collision != null
            && collision.gameObject.activeInHierarchy
            && IsPlayer(collision);
    }

    private static bool IsPlayer(Collider2D collision)
    {
        return collision.CompareTag("Player")
            || collision.GetComponentInParent<PlayerHealth2D>() != null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryCarry(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryCarry(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == carriedCollider)
        {
            ClearCarriedPlayer();
        }
    }

    private void TryCarry(Collider2D collision)
    {
        if (!CanCarry(collision) || !IsStandingOnPlatform(collision))
        {
            if (collision == carriedCollider)
            {
                ClearCarriedPlayer();
            }

            return;
        }

        carriedCollider = collision;
        carriedBody = collision.attachedRigidbody;
        carriedTransform = carriedBody != null ? carriedBody.transform : collision.transform;
    }

    private void CarryPlayer(Vector3 platformDelta)
    {
        if (carriedBody != null && carriedBody.bodyType != RigidbodyType2D.Static)
        {
            carriedBody.position += (Vector2)platformDelta;
            return;
        }

        carriedTransform.position += platformDelta;
    }

    private bool IsStandingOnPlatform(Collider2D playerCollider)
    {
        CachePlatformCollider();

        if (playerCollider == null || platformCollider == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Bounds platformBounds = platformCollider.bounds;
        bool overlapsX = playerBounds.max.x > platformBounds.min.x + horizontalInset
            && playerBounds.min.x < platformBounds.max.x - horizontalInset;
        bool closeAbove = playerBounds.min.y >= platformBounds.max.y - topContactTolerance
            && playerBounds.min.y <= platformBounds.max.y + topContactTolerance;
        return overlapsX && closeAbove;
    }

    private void CachePlatformCollider()
    {
        if (platformCollider != null && !platformCollider.isTrigger)
        {
            return;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D candidate in colliders)
        {
            if (candidate != null && !candidate.isTrigger)
            {
                platformCollider = candidate;
                return;
            }
        }
    }

    private void ClearCarriedPlayer()
    {
        carriedCollider = null;
        carriedBody = null;
        carriedTransform = null;
    }
}
