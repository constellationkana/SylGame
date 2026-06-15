using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1)]
    [SerializeField] private int startingHealth = 3;

    [Header("Disable On Death")]
    [SerializeField] private ThirdPersonController movementController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerAttack playerAttack;

    private int currentHealth;
    private bool isDead;

    public event System.Action<int> HealthChanged;

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

        if (movementController == null)
        {
            movementController = GetComponent<ThirdPersonController>();
        }

        if (playerInteraction == null)
        {
            playerInteraction = GetComponentInChildren<PlayerInteraction>();
        }

        if (playerAttack == null)
        {
            playerAttack = GetComponentInChildren<PlayerAttack>();
        }
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth);
    }

    private void Update()
    {
        if (isDead && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartCurrentLevel();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }

        int damageToApply = Mathf.Max(0, amount);
        if (damageToApply == 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damageToApply);
        HealthChanged?.Invoke(currentHealth);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void RestartCurrentLevel()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (movementController != null)
        {
            movementController.enabled = false;
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled = false;
        }

        if (playerAttack != null)
        {
            playerAttack.enabled = false;
        }
    }

    private void OnValidate()
    {
        startingHealth = Mathf.Max(1, startingHealth);
    }
}
