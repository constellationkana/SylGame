using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
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
    [Tooltip("How close the enemy gets before it stops chasing movement. Keep this at or below the attack range if the enemy should attack while stopped.")]
    [Min(0f)]
    [SerializeField] private float stoppingDistance = 1f;

    private int currentWaypointIndex;
    private bool isChasing;
    private NavMeshAgent navMeshAgent;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (enemyDetection == null)
        {
            enemyDetection = GetComponent<EnemyDetection>();
        }

        ConfigureAgentDefaults();
    }

    private void OnDisable()
    {
        StopNavigation();
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
            StopChasingAndResumePatrol();
        }

        Patrol();
    }

    private void Patrol()
    {
        Transform currentWaypoint = GetCurrentWaypoint();

        if (currentWaypoint == null)
        {
            StopNavigation();
            return;
        }

        if (HasReachedDestination(currentWaypoint))
        {
            AdvanceToNextWaypoint();
            return;
        }

        if (!CanUseAgent())
        {
            return;
        }

        navMeshAgent.speed = movementSpeed;
        navMeshAgent.stoppingDistance = 0f;
        SetDestination(currentWaypoint.position);
        RotateTowardAgentMovement();
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
        }

        Vector3 directionToPlayer = enemyDetection.Player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 moveDirection = directionToPlayer.normalized;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= stoppingDistance)
        {
            StopNavigation();
            RotateToward(moveDirection);
            return;
        }

        if (!CanUseAgent())
        {
            return;
        }

        navMeshAgent.speed = chaseSpeed;
        navMeshAgent.stoppingDistance = stoppingDistance;
        SetDestination(enemyDetection.Player.position);
        RotateTowardAgentMovement();
    }

    private void StopChasingAndResumePatrol()
    {
        isChasing = false;
        SetClosestWaypointAsCurrent();
        StopNavigation();
    }

    private void ConfigureAgentDefaults()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.updateRotation = false;
        navMeshAgent.autoBraking = true;
        navMeshAgent.speed = movementSpeed;
        navMeshAgent.stoppingDistance = 0f;
    }

    private bool CanUseAgent()
    {
        return navMeshAgent != null
            && navMeshAgent.enabled
            && navMeshAgent.isOnNavMesh;
    }

    private void SetDestination(Vector3 destination)
    {
        if (!CanUseAgent())
        {
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(destination);
    }

    private bool HasReachedDestination(Transform currentWaypoint)
    {
        if (currentWaypoint == null)
        {
            return false;
        }

        Vector3 waypointPosition = currentWaypoint.position;
        waypointPosition.y = transform.position.y;

        return Vector3.Distance(transform.position, waypointPosition) <= waypointReachDistance;
    }

    private void StopNavigation()
    {
        if (!CanUseAgent())
        {
            return;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    private void RotateTowardAgentMovement()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        Vector3 moveDirection = navMeshAgent.desiredVelocity;
        moveDirection.y = 0f;
        RotateToward(moveDirection);
    }

    private void SetClosestWaypointAsCurrent()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            currentWaypointIndex = 0;
            return;
        }

        int closestWaypointIndex = currentWaypointIndex;
        float closestDistanceSquared = float.PositiveInfinity;

        for (int i = 0; i < waypoints.Length; i++)
        {
            Transform waypoint = waypoints[i];

            if (waypoint == null)
            {
                continue;
            }

            Vector3 waypointPosition = waypoint.position;
            waypointPosition.y = transform.position.y;

            float distanceSquared = (waypointPosition - transform.position).sqrMagnitude;

            if (distanceSquared < closestDistanceSquared)
            {
                closestDistanceSquared = distanceSquared;
                closestWaypointIndex = i;
            }
        }

        currentWaypointIndex = closestWaypointIndex;
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
        stoppingDistance = Mathf.Max(0f, stoppingDistance);
    }
}
