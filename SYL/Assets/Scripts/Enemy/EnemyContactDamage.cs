using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [Header("Damage")]
    [Min(1)]
    [SerializeField] private int damageAmount = 1;
    [Tooltip("Seconds between damage ticks while the player stays in attack range.")]
    [Min(0.01f)]
    [SerializeField] private float damageInterval = 1f;

    [Header("Attack Range")]
    [Tooltip("Existing EnemyDetection component used to find the player.")]
    [SerializeField] private EnemyDetection enemyDetection;
    [Tooltip("How close the player must be before this enemy can damage them.")]
    [Min(0f)]
    [SerializeField] private float attackRange = 1.25f;

    private Transform cachedPlayerTransform;
    private PlayerHealth cachedPlayerHealth;
    private bool playerInAttackRange;
    private float nextDamageTime;
    private float attackRangeSquared;

    private void Awake()
    {
        CacheAttackRange();

        if (enemyDetection == null)
        {
            enemyDetection = GetComponent<EnemyDetection>();
        }
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPaused)
        {
            return;
        }

        Transform playerTransform = GetPlayerTransform();
        PlayerHealth playerHealth = GetPlayerHealth(playerTransform);

        if (playerTransform == null || playerHealth == null || !IsPlayerInAttackRange(playerTransform))
        {
            ResetAttack();
            return;
        }

        if (!playerInAttackRange)
        {
            playerInAttackRange = true;
            DamagePlayer(playerHealth);
            return;
        }

        if (Time.time >= nextDamageTime)
        {
            DamagePlayer(playerHealth);
        }
    }

    private Transform GetPlayerTransform()
    {
        if (enemyDetection == null)
        {
            return null;
        }

        return enemyDetection.Player;
    }

    private PlayerHealth GetPlayerHealth(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            cachedPlayerTransform = null;
            cachedPlayerHealth = null;
            return null;
        }

        if (cachedPlayerTransform != playerTransform)
        {
            cachedPlayerTransform = playerTransform;
            cachedPlayerHealth = playerTransform.GetComponentInParent<PlayerHealth>();
        }

        return cachedPlayerHealth;
    }

    private bool IsPlayerInAttackRange(Transform playerTransform)
    {
        Vector3 enemyToPlayer = playerTransform.position - transform.position;

        return enemyToPlayer.sqrMagnitude <= attackRangeSquared;
    }

    private void DamagePlayer(PlayerHealth playerHealth)
    {
        playerHealth.TakeDamage(damageAmount);
        nextDamageTime = Time.time + damageInterval;
    }

    private void ResetAttack()
    {
        playerInAttackRange = false;
        nextDamageTime = 0f;
    }

    private void OnValidate()
    {
        damageAmount = Mathf.Max(1, damageAmount);
        damageInterval = Mathf.Max(0.01f, damageInterval);
        attackRange = Mathf.Max(0f, attackRange);
        CacheAttackRange();
    }

    private void CacheAttackRange()
    {
        attackRangeSquared = attackRange * attackRange;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
