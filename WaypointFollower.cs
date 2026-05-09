using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointFollower : MonoBehaviour
{
    [SerializeField] private GameObject[] waypoints;
    private int currentWaypointIndex = 0;
    [SerializeField] private float speed = 2f;
    [SerializeField] private bool anchorWaypointsToStartPosition;
    private bool warnedMissingWaypoints;
    private bool anchorCached;
    private Vector3 anchorStartPosition;
    private Vector3 anchorReferenceWaypointPosition;

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            WarnMissingWaypoints();
            return;
        }

        currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);
        GameObject currentWaypoint = waypoints[currentWaypointIndex];

        if (currentWaypoint == null)
        {
            AdvanceWaypoint();
            return;
        }

        Vector3 targetPosition = GetTargetPositionForWaypoint(currentWaypoint);
        if (Vector2.Distance(targetPosition, transform.position) < .1f)
        {
            AdvanceWaypoint();
            currentWaypoint = waypoints[currentWaypointIndex];

            if (currentWaypoint == null)
            {
                return;
            }

            targetPosition = GetTargetPositionForWaypoint(currentWaypoint);
        }

        float moveSpeed = Mathf.Max(0f, speed);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, Time.deltaTime * moveSpeed);
    }

    private Vector3 GetTargetPositionForWaypoint(GameObject waypoint)
    {
        if (waypoint == null)
        {
            return transform.position;
        }

        if (!anchorWaypointsToStartPosition)
        {
            return waypoint.transform.position;
        }

        EnsureAnchorCache();
        return anchorStartPosition + (waypoint.transform.position - anchorReferenceWaypointPosition);
    }

    private void EnsureAnchorCache()
    {
        if (anchorCached)
        {
            return;
        }

        anchorStartPosition = transform.position;
        anchorReferenceWaypointPosition = anchorStartPosition;

        if (waypoints != null)
        {
            foreach (GameObject waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    anchorReferenceWaypointPosition = waypoint.transform.position;
                    break;
                }
            }
        }

        anchorCached = true;
    }

    private void AdvanceWaypoint()
    {
        currentWaypointIndex++;
        if (currentWaypointIndex >= waypoints.Length)
        {
            currentWaypointIndex = 0;
        }
    }

    private void WarnMissingWaypoints()
    {
        if (warnedMissingWaypoints)
        {
            return;
        }

        warnedMissingWaypoints = true;
        Debug.LogWarning($"{name} has no waypoints, so it will stay in place.", this);
    }
}
