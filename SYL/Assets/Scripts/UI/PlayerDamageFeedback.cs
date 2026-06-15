using UnityEngine;
using UnityEngine.UI;

public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image damageOverlay;

    [Header("Flash Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float flashAlpha = 0.35f;
    [Min(0f)]
    [SerializeField] private float holdDuration = 0.05f;
    [Min(0.01f)]
    [SerializeField] private float fadeDuration = 0.25f;

    private Color overlayColor = Color.red;
    private float holdTimer;
    private float fadeTimer;

    private void Awake()
    {
        if (damageOverlay == null)
        {
            damageOverlay = GetComponent<Image>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (damageOverlay != null)
        {
            overlayColor = damageOverlay.color;
            overlayColor.a = 0f;
            damageOverlay.color = overlayColor;
            damageOverlay.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.DamageTaken += HandleDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.DamageTaken -= HandleDamageTaken;
        }
    }

    private void Update()
    {
        if (damageOverlay == null || overlayColor.a <= 0f)
        {
            return;
        }

        if (holdTimer > 0f)
        {
            holdTimer -= Time.deltaTime;
            return;
        }

        fadeTimer += Time.deltaTime;
        float fadeProgress = Mathf.Clamp01(fadeTimer / fadeDuration);
        SetOverlayAlpha(Mathf.Lerp(flashAlpha, 0f, fadeProgress));
    }

    private void HandleDamageTaken(int damageAmount)
    {
        holdTimer = holdDuration;
        fadeTimer = 0f;
        SetOverlayAlpha(flashAlpha);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (damageOverlay == null)
        {
            return;
        }

        overlayColor.a = Mathf.Clamp01(alpha);
        damageOverlay.color = overlayColor;
    }

    private void OnValidate()
    {
        flashAlpha = Mathf.Clamp01(flashAlpha);
        holdDuration = Mathf.Max(0f, holdDuration);
        fadeDuration = Mathf.Max(0.01f, fadeDuration);
    }
}
