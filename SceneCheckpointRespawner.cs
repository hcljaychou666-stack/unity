using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SceneCheckpointRespawner : MonoBehaviour
{
    private Vector3? pendingCheckpointPosition;

    private void Awake()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (!PlayerLifeFlow.Instance.TryGetCheckpointPosition(sceneName, out Vector3 checkpointPosition))
        {
            return;
        }

        pendingCheckpointPosition = checkpointPosition;
        ApplyCheckpointPosition();
    }

    private IEnumerator Start()
    {
        if (!pendingCheckpointPosition.HasValue)
        {
            yield break;
        }

        yield return null;
        ApplyCheckpointPosition();
    }

    private void ApplyCheckpointPosition()
    {
        if (!pendingCheckpointPosition.HasValue)
        {
            return;
        }

        Vector3 checkpointPosition = ResolveGroundedPosition(pendingCheckpointPosition.Value);
        checkpointPosition.z = transform.position.z;
        transform.position = checkpointPosition;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.position = checkpointPosition;
            body.velocity = Vector2.zero;
        }

        Physics2D.SyncTransforms();
    }

    private Vector3 ResolveGroundedPosition(Vector3 checkpointPosition)
    {
        int groundMask = LayerMask.GetMask("Ground");

        if (groundMask == 0)
        {
            return checkpointPosition;
        }

        Collider2D playerCollider = GetComponent<Collider2D>();
        float playerHalfHeight = playerCollider != null
            ? playerCollider.bounds.extents.y
            : 0.85f;

        Vector2 probeOrigin = new Vector2(checkpointPosition.x, checkpointPosition.y + 2.5f);
        RaycastHit2D groundHit = Physics2D.Raycast(probeOrigin, Vector2.down, 6f, groundMask);

        if (groundHit.collider == null)
        {
            return checkpointPosition;
        }

        checkpointPosition.y = groundHit.point.y + playerHalfHeight + 0.05f;
        return checkpointPosition;
    }
}
