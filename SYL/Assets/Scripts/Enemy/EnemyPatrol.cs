using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints = new Transform[0];
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float waypointReachDistance = 0.25f;

    private int currentWaypointIndex;

    private void Update()
    {
        Transform currentWaypoint = GetCurrentWaypoint();

        if (currentWaypoint == null)
        {
            return;
        }

        Vector3 targetPosition = currentWaypoint.position;
        targetPosition.y = transform.position.y;

        Vector3 directionToWaypoint = targetPosition - transform.position;

        if (directionToWaypoint.magnitude <= waypointReachDistance)
        {
            AdvanceToNextWaypoint();
            return;
        }

        Vector3 moveDirection = directionToWaypoint.normalized;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            movementSpeed * Time.deltaTime);

        RotateToward(moveDirection);
    }

    private Transform GetCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return null;
        }

        if (currentWaypointIndex >= waypoints.Length)
        {
            currentWaypointIndex = 0;
        }

        Transform currentWaypoint = waypoints[currentWaypointIndex];

        if (currentWaypoint == null)
        {
            AdvanceToNextWaypoint();
            return null;
        }

        return currentWaypoint;
    }

    private void AdvanceToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            currentWaypointIndex = 0;
            return;
        }

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    private void RotateToward(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }
}
