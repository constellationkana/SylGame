using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1)]
    [SerializeField] private int startingHealth = 3;

    [Header("Disable On Death")]
    [Tooltip("Stops player detection when this enemy dies.")]
    [SerializeField] private EnemyDetection enemyDetection;
    [Tooltip("Stops patrol and chase movement when this enemy dies.")]
    [SerializeField] private EnemyPatrol enemyPatrol;
    [Tooltip("Stops this enemy from damaging the player when it dies.")]
    [SerializeField] private EnemyContactDamage enemyContactDamage;

    private int currentHealth;
    private bool isDead;

    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    public bool IsDead
    {
        get { return isDead; }
    }

    private void Awake()
    {
        currentHealth = startingHealth;
        AssignDefaultReferences();
    }

    private void Reset()
    {
        AssignDefaultReferences();
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead)
        {
            Debug.Log($"{gameObject.name} is already dead and cannot take more damage.", this);
            return;
        }

        int damageToApply = Mathf.Max(0, damageAmount);
        if (damageToApply == 0)
        {
            Debug.Log($"{gameObject.name} received no damage.", this);
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damageToApply);
        Debug.Log($"{gameObject.name} took {damageToApply} damage. Health: {currentHealth}/{startingHealth}", this);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Debug.Log($"{gameObject.name} died.", this);

        DisableEnemyBehaviors();
    }

    private void DisableEnemyBehaviors()
    {
        if (enemyDetection != null)
        {
            enemyDetection.enabled = false;
        }

        if (enemyPatrol != null)
        {
            enemyPatrol.enabled = false;
        }

        if (enemyContactDamage != null)
        {
            enemyContactDamage.enabled = false;
        }
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

        if (enemyContactDamage == null)
        {
            enemyContactDamage = GetComponent<EnemyContactDamage>();
        }
    }

    private void OnValidate()
    {
        startingHealth = Mathf.Max(1, startingHealth);
    }
}
