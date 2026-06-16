using System.Collections;
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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip deathClip;

    [Header("Damage Feedback")]
    [Tooltip("Color shown briefly when this enemy takes damage.")]
    [SerializeField] private Color damageFlashColor = Color.red;
    [Tooltip("How long the damage flash stays visible.")]
    [Min(0f)]
    [SerializeField] private float damageFlashDuration = 0.15f;

    [Header("Death Feedback")]
    [Tooltip("Color shown while the enemy waits to disappear after death.")]
    [SerializeField] private Color deathColor = new Color(0.25f, 0f, 0f);
    [Tooltip("How long the enemy remains visible after dying.")]
    [Min(0f)]
    [SerializeField] private float deathDelay = 1.5f;

    [Header("Visuals And Colliders")]
    [Tooltip("Renderers that flash when damaged. Leave empty to use renderers on this enemy and its children.")]
    [SerializeField] private Renderer[] enemyRenderers = new Renderer[0];
    [Tooltip("Colliders disabled when this enemy dies. Leave empty to use colliders on this enemy and its children.")]
    [SerializeField] private Collider[] enemyColliders = new Collider[0];

    private int currentHealth;
    private bool isDead;
    private Color[][] originalColors;
    private Coroutine damageFlashCoroutine;
    private const string MaterialColorPropertyName = "_Color";

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
        CacheOriginalColors();
    }

    private void Reset()
    {
        AssignDefaultReferences();
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead)
        {
            return;
        }

        int damageToApply = Mathf.Max(0, damageAmount);
        if (damageToApply == 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damageToApply);

        if (currentHealth == 0)
        {
            Die();
            return;
        }

        PlaySound(hitClip);
        ShowDamageFeedback();
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        PlaySound(deathClip);
        DisableEnemyBehaviors();
        DisableEnemyColliders();
        ShowDeathFeedback();
        StartCoroutine(DeactivateAfterDelay());
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

    private void DisableEnemyColliders()
    {
        foreach (Collider enemyCollider in enemyColliders)
        {
            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }
        }
    }

    private void ShowDamageFeedback()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }

        damageFlashCoroutine = StartCoroutine(DamageFlash());
    }

    private IEnumerator DamageFlash()
    {
        SetRendererColors(damageFlashColor);
        yield return new WaitForSeconds(damageFlashDuration);
        RestoreOriginalColors();
        damageFlashCoroutine = null;
    }

    private void ShowDeathFeedback()
    {
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = null;
        }

        SetRendererColors(deathColor);
    }

    private IEnumerator DeactivateAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);
        gameObject.SetActive(false);
    }

    private void CacheOriginalColors()
    {
        originalColors = new Color[enemyRenderers.Length][];

        for (int rendererIndex = 0; rendererIndex < enemyRenderers.Length; rendererIndex++)
        {
            Renderer enemyRenderer = enemyRenderers[rendererIndex];

            if (enemyRenderer == null)
            {
                originalColors[rendererIndex] = new Color[0];
                continue;
            }

            Material[] materials = enemyRenderer.materials;
            originalColors[rendererIndex] = new Color[materials.Length];

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material.HasProperty(MaterialColorPropertyName))
                {
                    originalColors[rendererIndex][materialIndex] = material.color;
                }
            }
        }
    }

    private void SetRendererColors(Color color)
    {
        foreach (Renderer enemyRenderer in enemyRenderers)
        {
            if (enemyRenderer == null)
            {
                continue;
            }

            Material[] materials = enemyRenderer.materials;
            foreach (Material material in materials)
            {
                if (material.HasProperty(MaterialColorPropertyName))
                {
                    material.color = color;
                }
            }
        }
    }

    private void RestoreOriginalColors()
    {
        for (int rendererIndex = 0; rendererIndex < enemyRenderers.Length; rendererIndex++)
        {
            Renderer enemyRenderer = enemyRenderers[rendererIndex];

            if (enemyRenderer == null || rendererIndex >= originalColors.Length)
            {
                continue;
            }

            Material[] materials = enemyRenderer.materials;
            Color[] rendererOriginalColors = originalColors[rendererIndex];

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];

                if (materialIndex < rendererOriginalColors.Length
                    && material.HasProperty(MaterialColorPropertyName))
                {
                    material.color = rendererOriginalColors[materialIndex];
                }
            }
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

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (enemyRenderers == null || enemyRenderers.Length == 0)
        {
            enemyRenderers = GetComponentsInChildren<Renderer>();
        }

        if (enemyColliders == null || enemyColliders.Length == 0)
        {
            enemyColliders = GetComponentsInChildren<Collider>();
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
        damageFlashDuration = Mathf.Max(0f, damageFlashDuration);
        deathDelay = Mathf.Max(0f, deathDelay);
    }
}
