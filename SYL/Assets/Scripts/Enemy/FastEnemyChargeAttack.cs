using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Adds a readable chase, warning, charge, and recovery cycle on top of shared enemy systems.
/// </summary>
[RequireComponent(typeof(EnemyDetection))]
[RequireComponent(typeof(EnemyPatrol))]
public class FastEnemyChargeAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyDetection enemyDetection;
    [SerializeField] private EnemyPatrol enemyPatrol;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private PlayerCameraController playerCameraController;

    [Header("Timing")]
    [Tooltip("How long this enemy chases normally before stopping to warn and charge.")]
    [Min(0f)]
    [SerializeField] private float chaseDuration = 1.5f;
    [Tooltip("How long this enemy stays stopped and flashing before charging.")]
    [Min(0f)]
    [SerializeField] private float warningDuration = 0.9f;
    [Tooltip("How long this enemy waits after charging before returning to normal AI.")]
    [Min(0f)]
    [SerializeField] private float recoveryDuration = 0.75f;

    [Header("Charge")]
    [Tooltip("Movement speed during the charge. High, but still dodgeable.")]
    [Min(0.01f)]
    [SerializeField] private float chargeSpeed = 14f;
    [Tooltip("Maximum charge travel time.")]
    [Min(0.01f)]
    [SerializeField] private float chargeDuration = 0.45f;
    [Tooltip("Maximum charge travel distance. The charge ends when this distance is reached or charge duration expires.")]
    [Min(0.01f)]
    [SerializeField] private float chargeDistance = 7f;
    [Tooltip("How quickly this enemy turns to face the player while warning.")]
    [Min(0f)]
    [SerializeField] private float warningTurnSpeed = 12f;

    [Header("Feedback")]
    [SerializeField] private Color warningFlashColor = Color.green;
    [Tooltip("Seconds between warning color toggles.")]
    [Min(0.01f)]
    [SerializeField] private float warningFlashInterval = 0.12f;
    [SerializeField] private AudioClip chargeClip;
    [Min(0f)]
    [SerializeField] private float cameraShakeDuration = 0.12f;
    [Min(0f)]
    [SerializeField] private float cameraShakeMagnitude = 0.12f;
    [Tooltip("Renderers that flash during the warning. Leave empty to use renderers on this enemy and its children.")]
    [SerializeField] private Renderer[] warningRenderers = new Renderer[0];

    private enum ChargeState
    {
        Idle,
        Chasing,
        Warning,
        Charging,
        Recovering
    }

    private const string MaterialColorPropertyName = "_Color";

    private ChargeState state;
    private float stateEndTime;
    private float nextFlashTime;
    private bool flashOn;
    private Vector3 chargeDirection;
    private Vector3 chargeStartPosition;
    private Vector3 chargeTargetPosition;
    private Color[][] originalColors;

    private void Awake()
    {
        AssignDefaultReferences();
        CacheOriginalColors();
    }

    private void Reset()
    {
        AssignDefaultReferences();
    }

    private void OnDisable()
    {
        RestoreOriginalColors();
        RestoreNavigationAfterCharge();
    }

    private void Update()
    {
        if (IsDead())
        {
            HandleDeath();
            return;
        }

        if (GameStateManager.Instance.IsPaused)
        {
            return;
        }

        switch (state)
        {
            case ChargeState.Idle:
                UpdateIdle();
                break;
            case ChargeState.Chasing:
                UpdateChasing();
                break;
            case ChargeState.Warning:
                UpdateWarning();
                break;
            case ChargeState.Charging:
                UpdateCharging();
                break;
            case ChargeState.Recovering:
                UpdateRecovering();
                break;
        }
    }

    private void UpdateIdle()
    {
        if (!CanSeePlayer())
        {
            return;
        }

        state = ChargeState.Chasing;
        stateEndTime = Time.time + chaseDuration;
        SetPatrolEnabled(true);
    }

    private void UpdateChasing()
    {
        if (!CanSeePlayer())
        {
            ResetCycle(true);
            return;
        }

        if (Time.time >= stateEndTime)
        {
            StartWarning();
        }
    }

    private void StartWarning()
    {
        state = ChargeState.Warning;
        stateEndTime = Time.time + warningDuration;
        nextFlashTime = 0f;
        flashOn = false;
        StopAgent();
        SetPatrolEnabled(false);
    }

    private void UpdateWarning()
    {
        Transform player = GetVisiblePlayer();
        if (player == null)
        {
            ResetCycle(true);
            return;
        }

        FacePosition(player.position);
        UpdateWarningFlash();

        if (Time.time >= stateEndTime)
        {
            StartCharge(player.position);
        }
    }

    private void StartCharge(Vector3 targetPosition)
    {
        RestoreOriginalColors();

        Vector3 flatTargetPosition = targetPosition;
        flatTargetPosition.y = transform.position.y;

        Vector3 directionToTarget = flatTargetPosition - transform.position;
        if (directionToTarget.sqrMagnitude <= 0.001f)
        {
            directionToTarget = transform.forward;
        }

        chargeDirection = directionToTarget.normalized;
        chargeStartPosition = transform.position;
        chargeTargetPosition = flatTargetPosition;
        state = ChargeState.Charging;
        stateEndTime = Time.time + chargeDuration;

        PlayChargeFeedback();
        StopAgent();
        SetAgentEnabled(false);
    }

    private void UpdateCharging()
    {
        float stepDistance = chargeSpeed * Time.deltaTime;
        MoveCharge(stepDistance);
        FacePosition(transform.position + chargeDirection);

        float traveledDistance = Vector3.Distance(chargeStartPosition, transform.position);
        bool reachedConfiguredDistance = traveledDistance >= chargeDistance;
        bool reachedTarget = Vector3.Dot(chargeTargetPosition - transform.position, chargeDirection) <= 0f;

        if (Time.time >= stateEndTime || reachedConfiguredDistance || reachedTarget)
        {
            StartRecovery();
        }
    }

    private void StartRecovery()
    {
        state = ChargeState.Recovering;
        stateEndTime = Time.time + recoveryDuration;
        RestoreNavigationAfterCharge();
        StopAgent();
    }

    private void UpdateRecovering()
    {
        if (Time.time < stateEndTime)
        {
            return;
        }

        ResetCycle(CanSeePlayer());
    }

    private void ResetCycle(bool resumePatrol)
    {
        RestoreOriginalColors();
        RestoreNavigationAfterCharge();
        SetPatrolEnabled(resumePatrol);
        state = ChargeState.Idle;
        stateEndTime = 0f;
        nextFlashTime = 0f;
        flashOn = false;
    }

    private void UpdateWarningFlash()
    {
        if (Time.time < nextFlashTime)
        {
            return;
        }

        flashOn = !flashOn;
        SetRendererColors(flashOn ? warningFlashColor : GetFirstOriginalColor());
        nextFlashTime = Time.time + warningFlashInterval;
    }

    private bool CanSeePlayer()
    {
        return GetVisiblePlayer() != null;
    }

    private Transform GetVisiblePlayer()
    {
        if (enemyDetection == null
            || !enemyDetection.enabled
            || !enemyDetection.PlayerDetected
            || enemyDetection.Player == null)
        {
            return null;
        }

        PlayerHealth playerHealth = enemyDetection.Player.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null && playerHealth.IsDead)
        {
            return null;
        }

        return enemyDetection.Player;
    }

    private bool IsDead()
    {
        return enemyHealth != null && enemyHealth.IsDead;
    }

    private void HandleDeath()
    {
        RestoreOriginalColors();

        if (state == ChargeState.Charging)
        {
            RestoreNavigationAfterCharge();
        }

        StopAgent();
        state = ChargeState.Idle;
        enabled = false;
    }

    private void MoveCharge(float distance)
    {
        Vector3 motion = chargeDirection * distance;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(motion);
            return;
        }

        transform.position += motion;
    }

    private void FacePosition(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            warningTurnSpeed * Time.deltaTime);
    }

    private void StopAgent()
    {
        if (!CanUseAgent())
        {
            return;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    private void RestoreNavigationAfterCharge()
    {
        SetAgentEnabled(true);

        if (CanUseAgent())
        {
            navMeshAgent.Warp(transform.position);
            navMeshAgent.isStopped = false;
        }
    }

    private void SetAgentEnabled(bool isEnabled)
    {
        if (navMeshAgent != null && navMeshAgent.enabled != isEnabled)
        {
            navMeshAgent.enabled = isEnabled;
        }
    }

    private bool CanUseAgent()
    {
        return navMeshAgent != null
            && navMeshAgent.enabled
            && navMeshAgent.isOnNavMesh;
    }

    private void SetPatrolEnabled(bool isEnabled)
    {
        if (enemyPatrol != null && enemyPatrol.enabled != isEnabled)
        {
            enemyPatrol.enabled = isEnabled;
        }
    }

    private void PlayChargeFeedback()
    {
        if (audioSource != null && chargeClip != null)
        {
            audioSource.PlayOneShot(chargeClip);
        }

        PlayerCameraController cameraController = GetPlayerCameraController();
        if (cameraController != null && cameraShakeDuration > 0f && cameraShakeMagnitude > 0f)
        {
            cameraController.Shake(cameraShakeDuration, cameraShakeMagnitude);
        }
    }

    private PlayerCameraController GetPlayerCameraController()
    {
        if (playerCameraController != null)
        {
            return playerCameraController;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerCameraController = mainCamera.GetComponent<PlayerCameraController>();
            if (playerCameraController != null)
            {
                return playerCameraController;
            }
        }

        playerCameraController = FindObjectOfType<PlayerCameraController>();
        return playerCameraController;
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

        if (enemyHealth == null)
        {
            enemyHealth = GetComponent<EnemyHealth>();
        }

        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponentInChildren<AudioSource>();
        }

        if (warningRenderers == null || warningRenderers.Length == 0)
        {
            warningRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    private void CacheOriginalColors()
    {
        originalColors = new Color[warningRenderers.Length][];

        for (int rendererIndex = 0; rendererIndex < warningRenderers.Length; rendererIndex++)
        {
            Renderer warningRenderer = warningRenderers[rendererIndex];

            if (warningRenderer == null)
            {
                originalColors[rendererIndex] = new Color[0];
                continue;
            }

            Material[] materials = warningRenderer.materials;
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
        foreach (Renderer warningRenderer in warningRenderers)
        {
            if (warningRenderer == null)
            {
                continue;
            }

            Material[] materials = warningRenderer.materials;
            foreach (Material material in materials)
            {
                if (material.HasProperty(MaterialColorPropertyName))
                {
                    material.color = color;
                }
            }
        }
    }

    private Color GetFirstOriginalColor()
    {
        if (originalColors == null)
        {
            return Color.white;
        }

        for (int rendererIndex = 0; rendererIndex < originalColors.Length; rendererIndex++)
        {
            if (originalColors[rendererIndex] != null && originalColors[rendererIndex].Length > 0)
            {
                return originalColors[rendererIndex][0];
            }
        }

        return Color.white;
    }

    private void RestoreOriginalColors()
    {
        if (originalColors == null)
        {
            return;
        }

        for (int rendererIndex = 0; rendererIndex < warningRenderers.Length; rendererIndex++)
        {
            Renderer warningRenderer = warningRenderers[rendererIndex];

            if (warningRenderer == null || rendererIndex >= originalColors.Length)
            {
                continue;
            }

            Material[] materials = warningRenderer.materials;
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

    private void OnValidate()
    {
        chaseDuration = Mathf.Max(0f, chaseDuration);
        warningDuration = Mathf.Max(0f, warningDuration);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        chargeSpeed = Mathf.Max(0.01f, chargeSpeed);
        chargeDuration = Mathf.Max(0.01f, chargeDuration);
        chargeDistance = Mathf.Max(0.01f, chargeDistance);
        warningTurnSpeed = Mathf.Max(0f, warningTurnSpeed);
        warningFlashInterval = Mathf.Max(0.01f, warningFlashInterval);
        cameraShakeDuration = Mathf.Max(0f, cameraShakeDuration);
        cameraShakeMagnitude = Mathf.Max(0f, cameraShakeMagnitude);
    }
}
