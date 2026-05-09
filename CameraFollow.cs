using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerBody;

    [Header("Follow")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 0.85f);
    [SerializeField] private float smoothTime = 0.12f;

    [Header("Look Ahead")]
    [SerializeField] private float lookAheadDistance = 1.35f;
    [SerializeField] private float lookAheadSmoothTime = 0.18f;
    [SerializeField] private float lookAheadVelocityThreshold = 0.08f;

    [Header("Bounds")]
    [SerializeField] private bool useCameraBounds = true;
    [SerializeField] private Vector2 levelBoundsMin = new Vector2(-12f, -5f);
    [SerializeField] private Vector2 levelBoundsMax = new Vector2(64f, 8.5f);
    [SerializeField] private bool relaxBoundsNearEdges = true;
    [SerializeField] private Vector2 edgeFollowPadding = new Vector2(10f, 6f);
    [SerializeField] private float teleportResetDistance = 4f;

    [Header("Pixel Art")]
    [SerializeField] private bool snapToPixelGrid = false;
    [SerializeField] private float pixelSnapReferencePixelsPerUnit = 100f;

    [Header("Screen Shake")]
    [SerializeField] private bool enableScreenShake = false;
    [SerializeField] private float defaultShakeIntensity = 0f;
    [SerializeField] private float defaultShakeDuration = 0.12f;

    private Vector3 velocity;
    private Camera followCamera;
    private Vector3 lastPlayerPosition;
    private bool hasLastPlayerPosition;
    private float lookAheadX;
    private float lookAheadVelocity;
    private float shakeRemaining;
    private float shakeDuration;
    private float shakeIntensity;

    private void Awake()
    {
        CacheReferences();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        CacheReferences();

        Vector3 playerPosition = player.position;
        if (hasLastPlayerPosition
            && teleportResetDistance > 0f
            && Vector3.Distance(playerPosition, lastPlayerPosition) > teleportResetDistance)
        {
            velocity = Vector3.zero;
            lookAheadX = 0f;
            lookAheadVelocity = 0f;
        }

        float movementDirection = GetHorizontalMovementDirection(playerPosition);
        float targetLookAhead = movementDirection * lookAheadDistance;
        lookAheadX = Mathf.SmoothDamp(
            lookAheadX,
            targetLookAhead,
            ref lookAheadVelocity,
            Mathf.Max(0.01f, lookAheadSmoothTime));

        Vector3 targetPosition = new Vector3(
            playerPosition.x + offset.x + lookAheadX,
            playerPosition.y + offset.y,
            transform.position.z);

        targetPosition = ClampToCameraBounds(targetPosition, playerPosition);

        if (smoothTime <= 0f)
        {
            transform.position = SnapToPixelGrid(targetPosition + GetShakeOffset());
            RememberPlayerPosition(playerPosition);
            return;
        }

        Vector3 smoothPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        transform.position = SnapToPixelGrid(smoothPosition + GetShakeOffset());
        RememberPlayerPosition(playerPosition);
    }

    public void TriggerShake(float intensity, float duration)
    {
        if (!enableScreenShake || intensity <= 0f || duration <= 0f)
        {
            return;
        }

        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeRemaining = Mathf.Max(shakeRemaining, duration);
    }

    public void TriggerDefaultShake()
    {
        TriggerShake(defaultShakeIntensity, defaultShakeDuration);
    }

    public void TriggerShake(Vector2 origin, float intensity, float duration)
    {
        if (!enableScreenShake || intensity <= 0f || duration <= 0f)
        {
            return;
        }

        float distance = Vector2.Distance(origin, transform.position);
        float falloff = Mathf.Clamp01(1f - (distance / 12f));
        TriggerShake(intensity * falloff, duration);
    }

    private void CacheReferences()
    {
        if (followCamera == null)
        {
            followCamera = GetComponent<Camera>();
        }

        if (playerBody == null && player != null)
        {
            playerBody = player.GetComponent<Rigidbody2D>();
        }
    }

    private float GetHorizontalMovementDirection(Vector3 playerPosition)
    {
        if (playerBody != null && Mathf.Abs(playerBody.velocity.x) > lookAheadVelocityThreshold)
        {
            return Mathf.Sign(playerBody.velocity.x);
        }

        if (!hasLastPlayerPosition || Time.deltaTime <= 0f)
        {
            return 0f;
        }

        float estimatedVelocityX = (playerPosition.x - lastPlayerPosition.x) / Time.deltaTime;
        return Mathf.Abs(estimatedVelocityX) > lookAheadVelocityThreshold
            ? Mathf.Sign(estimatedVelocityX)
            : 0f;
    }

    private Vector3 ClampToCameraBounds(Vector3 targetPosition, Vector3 playerPosition)
    {
        if (!useCameraBounds)
        {
            return targetPosition;
        }

        float halfHeight = 0f;
        float halfWidth = 0f;

        if (followCamera != null && followCamera.orthographic)
        {
            halfHeight = followCamera.orthographicSize;
            halfWidth = halfHeight * followCamera.aspect;
        }

        Vector2 effectiveMin = levelBoundsMin;
        Vector2 effectiveMax = levelBoundsMax;

        if (relaxBoundsNearEdges)
        {
            ApplyEdgeRelaxation(playerPosition, halfWidth, halfHeight, ref effectiveMin, ref effectiveMax);
        }

        targetPosition.x = ClampWithExtent(targetPosition.x, effectiveMin.x, effectiveMax.x, halfWidth);
        targetPosition.y = ClampWithExtent(targetPosition.y, effectiveMin.y, effectiveMax.y, halfHeight);
        return targetPosition;
    }

    private void ApplyEdgeRelaxation(
        Vector3 playerPosition,
        float halfWidth,
        float halfHeight,
        ref Vector2 effectiveMin,
        ref Vector2 effectiveMax)
    {
        if (playerPosition.x < levelBoundsMin.x + halfWidth)
        {
            effectiveMin.x -= edgeFollowPadding.x;
        }
        else if (playerPosition.x > levelBoundsMax.x - halfWidth)
        {
            effectiveMax.x += edgeFollowPadding.x;
        }

        if (playerPosition.y < levelBoundsMin.y + halfHeight)
        {
            effectiveMin.y -= edgeFollowPadding.y;
        }
        else if (playerPosition.y > levelBoundsMax.y - halfHeight)
        {
            effectiveMax.y += edgeFollowPadding.y;
        }
    }

    private static float ClampWithExtent(float value, float min, float max, float extent)
    {
        float minCenter = min + extent;
        float maxCenter = max - extent;

        if (maxCenter < minCenter)
        {
            return (min + max) * 0.5f;
        }

        return Mathf.Clamp(value, minCenter, maxCenter);
    }

    private void RememberPlayerPosition(Vector3 playerPosition)
    {
        lastPlayerPosition = playerPosition;
        hasLastPlayerPosition = true;
    }

    private Vector3 SnapToPixelGrid(Vector3 position)
    {
        if (!snapToPixelGrid)
        {
            return position;
        }

        float unitsPerPixel = GetWorldUnitsPerPixel();
        if (unitsPerPixel <= 0f)
        {
            return position;
        }

        position.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
        position.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
        return position;
    }

    private float GetWorldUnitsPerPixel()
    {
        if (followCamera != null && followCamera.orthographic && Screen.height > 0)
        {
            return followCamera.orthographicSize * 2f / Screen.height;
        }

        return pixelSnapReferencePixelsPerUnit > 0f ? 1f / pixelSnapReferencePixelsPerUnit : 0f;
    }

    private Vector3 GetShakeOffset()
    {
        if (!enableScreenShake || shakeRemaining <= 0f || shakeIntensity <= 0f)
        {
            shakeRemaining = 0f;
            return Vector3.zero;
        }

        shakeRemaining -= Time.unscaledDeltaTime;
        float normalizedTime = shakeDuration > 0f ? Mathf.Clamp01(shakeRemaining / shakeDuration) : 0f;
        Vector2 randomOffset = Random.insideUnitCircle * (shakeIntensity * normalizedTime);

        if (shakeRemaining <= 0f)
        {
            shakeIntensity = 0f;
            shakeDuration = 0f;
        }

        return new Vector3(randomOffset.x, randomOffset.y, 0f);
    }
}
