using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints = new Transform[0];
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float waypointReachDistance = 0.25f;

    [Header("Chase")]
    [Tooltip("Existing EnemyDetection component used to know when the player is in range.")]
    [SerializeField] private EnemyDetection enemyDetection;
    [Tooltip("How fast the enemy moves while chasing the player.")]
    [Min(0f)]
    [SerializeField] private float chaseSpeed = 4f;

    private int currentWaypointIndex;
    private bool isChasing;

    private void Awake()
    {
        if (enemyDetection == null)
        {
            enemyDetection = GetComponent<EnemyDetection>();
        }
    }

    private void Update()
    {
        if (ShouldChasePlayer())
        {
            ChasePlayer();
            return;
        }

        if (isChasing)
        {
            isChasing = false;
            Debug.Log("Enemy leaving chase mode. Resuming patrol.", this);
        }

        Patrol();
    }

    private void Patrol()
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

    private bool ShouldChasePlayer()
    {
        return enemyDetection != null
            && enemyDetection.PlayerDetected
            && enemyDetection.Player != null;
    }

    private void ChasePlayer()
    {
        if (!isChasing)
        {
            isChasing = true;
            Debug.Log("Enemy entering chase mode.", this);
        }

        Vector3 targetPosition = enemyDetection.Player.position;
        targetPosition.y = transform.position.y;

        Vector3 directionToPlayer = targetPosition - transform.position;

        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 moveDirection = directionToPlayer.normalized;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            chaseSpeed * Time.deltaTime);

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

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        waypointReachDistance = Mathf.Max(0f, waypointReachDistance);
        chaseSpeed = Mathf.Max(0f, chaseSpeed);
    }
}
