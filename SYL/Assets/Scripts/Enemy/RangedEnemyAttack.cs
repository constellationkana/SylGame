using UnityEngine;

/// <summary>
/// Adds a simple ranged attack on top of the existing enemy detection and patrol behavior.
/// </summary>
public class RangedEnemyAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyDetection enemyDetection;
    [SerializeField] private EnemyPatrol enemyPatrol;
    [SerializeField] private Transform projectileOrigin;
    [SerializeField] private Material projectileMaterial;

    [Header("Attack")]
    [Min(0f)]
    [SerializeField] private float attackRange = 6f;
    [Min(0.01f)]
    [SerializeField] private float fireCooldown = 1.25f;
    [Min(0.01f)]
    [SerializeField] private float projectileSpeed = 9f;
    [Min(1)]
    [SerializeField] private int projectileDamage = 1;

    [Header("Projectile")]
    [Min(0.01f)]
    [SerializeField] private float projectileLifetime = 4f;
    [Min(0.01f)]
    [SerializeField] private float projectileRadius = 0.18f;
    [SerializeField] private float aimHeightOffset = 1f;
    [Min(0f)]
    [SerializeField] private float facePlayerSpeed = 10f;

    private float attackRangeSquared;
    private float nextFireTime;
    private EnemyTimePauseController timePauseController;

    private void Awake()
    {
        AssignDefaultReferences();
        CacheAttackRange();
        SyncPatrolStoppingDistance();
    }

    private void OnEnable()
    {
        if (timePauseController != null)
        {
            timePauseController.Resumed.AddListener(HandleTimeResumed);
        }
    }

    private void OnDisable()
    {
        if (timePauseController != null)
        {
            timePauseController.Resumed.RemoveListener(HandleTimeResumed);
        }
    }

    private void Reset()
    {
        AssignDefaultReferences();
        SyncPatrolStoppingDistance();
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPaused || IsTimePaused() || enemyDetection == null || !enemyDetection.enabled)
        {
            return;
        }

        Transform player = enemyDetection.Player;
        PlayerHealth playerHealth = GetPlayerHealth(player);
        if (!enemyDetection.PlayerDetected
            || player == null
            || (playerHealth != null && playerHealth.IsDead)
            || !IsPlayerInAttackRange(player))
        {
            ResetAttack();
            return;
        }

        FacePlayer(player);

        if (Time.time >= nextFireTime)
        {
            FireAt(player);
        }
    }

    private void FireAt(Transform player)
    {
        Vector3 spawnPosition = GetProjectileStartPosition();
        Vector3 targetPosition = player.position + Vector3.up * aimHeightOffset;
        Vector3 fireDirection = targetPosition - spawnPosition;

        if (fireDirection.sqrMagnitude <= 0.001f)
        {
            fireDirection = transform.forward;
        }

        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "EnemyProjectile";
        projectileObject.transform.position = spawnPosition;
        projectileObject.transform.localScale = Vector3.one * (projectileRadius * 2f);

        Collider projectileCollider = projectileObject.GetComponent<Collider>();
        if (projectileCollider != null)
        {
            projectileCollider.isTrigger = true;
        }

        Rigidbody projectileRigidbody = projectileObject.AddComponent<Rigidbody>();
        projectileRigidbody.isKinematic = true;
        projectileRigidbody.useGravity = false;

        Renderer projectileRenderer = projectileObject.GetComponent<Renderer>();
        if (projectileRenderer != null && projectileMaterial != null)
        {
            projectileRenderer.sharedMaterial = projectileMaterial;
        }

        EnemyProjectile projectile = projectileObject.AddComponent<EnemyProjectile>();
        projectile.Initialize(
            fireDirection.normalized,
            projectileSpeed,
            projectileDamage,
            projectileLifetime,
            transform);

        nextFireTime = Time.time + fireCooldown;
    }

    private bool IsPlayerInAttackRange(Transform player)
    {
        Vector3 enemyToPlayer = player.position - transform.position;
        return enemyToPlayer.sqrMagnitude <= attackRangeSquared;
    }

    private PlayerHealth GetPlayerHealth(Transform player)
    {
        if (player == null)
        {
            return null;
        }

        return player.GetComponentInParent<PlayerHealth>();
    }

    private void FacePlayer(Transform player)
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            facePlayerSpeed * Time.deltaTime);
    }

    private Vector3 GetProjectileStartPosition()
    {
        if (projectileOrigin != null)
        {
            return projectileOrigin.position;
        }

        return transform.position + Vector3.up * aimHeightOffset + transform.forward * 0.6f;
    }

    private void ResetAttack()
    {
        nextFireTime = 0f;
    }

    private void AssignDefaultReferences()
    {
        if (enemyDetection == null)
        {
            enemyDetection = GetComponent<EnemyDetection>();
        }

        if (enemyPatrol == null)
        {
            enemyPatrol = GetComponent<EnemyPatrol>();
        }

        if (timePauseController == null)
        {
            timePauseController = GetComponent<EnemyTimePauseController>();
        }
    }

    private void SyncPatrolStoppingDistance()
    {
        if (enemyPatrol != null)
        {
            enemyPatrol.SetChaseStoppingDistance(attackRange);
        }
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0f, attackRange);
        fireCooldown = Mathf.Max(0.01f, fireCooldown);
        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        projectileDamage = Mathf.Max(1, projectileDamage);
        projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
        projectileRadius = Mathf.Max(0.01f, projectileRadius);
        facePlayerSpeed = Mathf.Max(0f, facePlayerSpeed);
        CacheAttackRange();
        SyncPatrolStoppingDistance();
    }

    private void CacheAttackRange()
    {
        attackRangeSquared = attackRange * attackRange;
    }

    private bool IsTimePaused()
    {
        return timePauseController != null && timePauseController.IsTimePaused;
    }

    private void HandleTimeResumed()
    {
        if (nextFireTime > 0f && timePauseController != null)
        {
            nextFireTime += Time.time - timePauseController.LastPauseStartedAt;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
