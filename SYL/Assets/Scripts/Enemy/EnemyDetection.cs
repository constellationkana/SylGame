using UnityEngine;

/// <summary>
/// Detects whether the assigned player is within range and visible through line of sight.
/// </summary>
public class EnemyDetection : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("The player Transform this enemy checks distance against.")]
    [SerializeField] private Transform player;
    [Tooltip("How close the player must be before this enemy reports detection.")]
    [Min(0f)]
    [SerializeField] private float detectionRadius = 5f;
    [Tooltip("Optional point to raycast from. If empty, the enemy position plus 1 meter upward is used.")]
    [SerializeField] private Transform visionOrigin;
    [Tooltip("Layers that block this enemy's view, such as walls.")]
    [SerializeField] private LayerMask visionBlockingLayers;
    [Tooltip("Draws the line-of-sight ray in the Scene view while the game is running.")]
    [SerializeField] private bool showVisionDebug;

    private bool playerDetected;
    private float detectionRadiusSquared;
    private Transform cachedPlayerTransform;
    private PlayerHealth cachedPlayerHealth;

    /// <summary>
    /// Gets whether the player is currently within detection range and line of sight.
    /// </summary>
    public bool PlayerDetected
    {
        get { return playerDetected; }
    }

    /// <summary>
    /// Gets the player transform this detector is tracking.
    /// </summary>
    public Transform Player
    {
        get { return player; }
    }

    private void Awake()
    {
        CacheDetectionRadius();
    }

    private void Update()
    {
        if (player == null || !IsPlayerAlive())
        {
            if (playerDetected)
            {
                playerDetected = false;
            }

            return;
        }

        bool playerIsVisible = IsPlayerInRange() && HasLineOfSightToPlayer();

        if (playerIsVisible == playerDetected)
        {
            return;
        }

        playerDetected = playerIsVisible;
    }

    private bool IsPlayerInRange()
    {
        Vector3 enemyToPlayer = player.position - transform.position;

        return enemyToPlayer.sqrMagnitude <= detectionRadiusSquared;
    }

    private bool HasLineOfSightToPlayer()
    {
        Vector3 rayStart = GetVisionStartPosition();
        Vector3 rayEnd = player.position + Vector3.up;
        Vector3 directionToPlayer = rayEnd - rayStart;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= 0.001f)
        {
            return true;
        }

        bool isBlocked = Physics.Raycast(
            rayStart,
            directionToPlayer.normalized,
            distanceToPlayer,
            visionBlockingLayers,
            QueryTriggerInteraction.Ignore);

        if (showVisionDebug)
        {
            Color rayColor = isBlocked ? Color.red : Color.green;
            Debug.DrawRay(rayStart, directionToPlayer, rayColor);
        }

        return !isBlocked;
    }

    private bool IsPlayerAlive()
    {
        PlayerHealth playerHealth = GetPlayerHealth();

        return playerHealth == null || !playerHealth.IsDead;
    }

    private PlayerHealth GetPlayerHealth()
    {
        if (player == null)
        {
            cachedPlayerTransform = null;
            cachedPlayerHealth = null;
            return null;
        }

        if (cachedPlayerTransform != player)
        {
            cachedPlayerTransform = player;
            cachedPlayerHealth = player.GetComponentInParent<PlayerHealth>();
        }

        return cachedPlayerHealth;
    }

    private Vector3 GetVisionStartPosition()
    {
        if (visionOrigin != null)
        {
            return visionOrigin.position;
        }

        return transform.position + Vector3.up;
    }

    private void OnValidate()
    {
        detectionRadius = Mathf.Max(0f, detectionRadius);
        CacheDetectionRadius();
    }

    private void CacheDetectionRadius()
    {
        detectionRadiusSquared = detectionRadius * detectionRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (!showVisionDebug || player == null)
        {
            return;
        }

        Vector3 rayStart = GetVisionStartPosition();
        Vector3 rayEnd = player.position + Vector3.up;

        Gizmos.color = playerDetected ? Color.green : Color.red;
        Gizmos.DrawLine(rayStart, rayEnd);
    }
}
