using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks player health, raises health events, and handles game-over restart flow.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1)]
    [SerializeField] private int startingHealth = 3;

    [Header("Disable On Death")]
    [SerializeField] private ThirdPersonController movementController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private PlayerAttack playerAttack;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageClip;
    [SerializeField] private AudioClip gameOverClip;

    private int currentHealth;
    private bool isDead;

    /// <summary>
    /// Raised whenever the player's current health changes.
    /// </summary>
    public event System.Action<int> HealthChanged;

    /// <summary>
    /// Raised whenever positive damage is applied to the player.
    /// </summary>
    public event System.Action<int> DamageTaken;

    /// <summary>
    /// Gets the player's current health value.
    /// </summary>
    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    /// <summary>
    /// Gets whether the player has entered the game-over state.
    /// </summary>
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

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Reset()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth);
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPaused)
        {
            return;
        }

        if (isDead && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartCurrentLevel();
        }
    }

    /// <summary>
    /// Applies damage to the player and triggers death when health reaches zero.
    /// </summary>
    /// <param name="amount">Amount of damage requested. Negative values are ignored.</param>
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
        PlaySound(damageClip);
        DamageTaken?.Invoke(damageToApply);
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
        GameStateManager.Instance.SetState(GameState.GameOver);
        PlaySound(gameOverClip);

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

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void OnValidate()
    {
        startingHealth = Mathf.Max(1, startingHealth);
    }
}
