using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RamDashAbility : MonoBehaviour
{
    public enum RamDashDisplayPhase
    {
        Ready,
        Charging,
        Charged,
        Dashing,
        CooldownDraining,
        CooldownFilling
    }

    [Header("Input")]
    [SerializeField] private InputActionReference chargeAction;
    [SerializeField] private Key keyboardFallbackKey = Key.R;

    [Header("Ram Dash")]
    [Min(0f)]
    [SerializeField] private float chargeTime = 0.75f;
    [Min(0.01f)]
    [SerializeField] private float dashSpeed = 16f;
    [Min(0.01f)]
    [SerializeField] private float dashDuration = 0.28f;
    [Min(1)]
    [SerializeField] private int damage = 1;
    [Min(0f)]
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private bool requireGrounded = true;

    [Header("Hit Detection")]
    [Min(0.01f)]
    [SerializeField] private float hitRadius = 0.75f;
    [SerializeField] private Vector3 hitOffset = new Vector3(0f, 0.9f, 0f);
    [SerializeField] private LayerMask enemyLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Pass Through")]
    [SerializeField] private bool ignoreEnemyCollisionsDuringDash = true;

    [Header("Invulnerability")]
    [Min(0f)]
    [SerializeField] private float iframeStartDelay = 0f;
    [Min(0f)]
    [SerializeField] private float iframeDuration = 0.35f;

    [Header("Camera Feedback")]
    [SerializeField] private PlayerCameraController playerCameraController;
    [Min(0f)]
    [SerializeField] private float cameraShakeIntensity = 0.12f;
    [Min(0f)]
    [SerializeField] private float cameraShakeDuration = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip readySound;
    [SerializeField] private AudioClip ramDashStartSound;
    [SerializeField] private AudioClip ramDashHitEnemySound;

    [Header("Events")]
    [SerializeField] private UnityEvent chargeStarted = new UnityEvent();
    [SerializeField] private UnityEvent chargeCanceled = new UnityEvent();
    [SerializeField] private UnityEvent fullyCharged = new UnityEvent();
    [SerializeField] private UnityEvent dashStarted = new UnityEvent();
    [SerializeField] private UnityEvent dashEnded = new UnityEvent();
    [SerializeField] private UnityEvent<EnemyHealth> enemyHit = new UnityEvent<EnemyHealth>();

    private readonly Collider[] overlapResults = new Collider[64];
    private readonly RaycastHit[] castResults = new RaycastHit[64];
    private readonly HashSet<EnemyHealth> damagedEnemies = new HashSet<EnemyHealth>();
    private readonly HashSet<EnemyHealth> ignoredEnemies = new HashSet<EnemyHealth>();
    private readonly List<IgnoredCollisionPair> ignoredCollisionPairs = new List<IgnoredCollisionPair>();

    private CharacterController characterController;
    private ThirdPersonController playerController;
    private PlayerHealth playerHealth;
    private Collider[] playerColliders;
    private float currentChargeTime;
    private float dashTimeRemaining;
    private float cooldownRemaining;
    private Vector3 dashDirection;
    private Coroutine invulnerabilityCoroutine;
    private bool isCharging;
    private bool isFullyCharged;
    private bool readySoundPlayed;

    private struct IgnoredCollisionPair
    {
        public Collider PlayerCollider;
        public Collider EnemyCollider;

        public IgnoredCollisionPair(Collider playerCollider, Collider enemyCollider)
        {
            PlayerCollider = playerCollider;
            EnemyCollider = enemyCollider;
        }
    }

    public float ChargeTime
    {
        get { return chargeTime; }
    }

    public float DashSpeed
    {
        get { return dashSpeed; }
    }

    public float DashDuration
    {
        get { return dashDuration; }
    }

    public int Damage
    {
        get { return damage; }
    }

    public float Cooldown
    {
        get { return cooldown; }
    }

    public float CooldownRemaining
    {
        get { return cooldownRemaining; }
    }

    public bool IsSharedInputMode
    {
        get
        {
            ResolvePlayerController();
            return IsSharedWithNormalDash(playerController != null ? playerController.DashInputAction : null);
        }
    }

    public bool IsCharging
    {
        get { return isCharging; }
    }

    public bool IsFullyCharged
    {
        get { return isFullyCharged; }
    }

    public bool IsDashing
    {
        get { return dashTimeRemaining > 0f; }
    }

    public bool IsReady
    {
        get { return cooldownRemaining <= 0f && !IsDashing && !isCharging; }
    }

    public RamDashDisplayPhase DisplayPhase
    {
        get
        {
            if (isCharging)
            {
                return isFullyCharged ? RamDashDisplayPhase.Charged : RamDashDisplayPhase.Charging;
            }

            if (IsDashing)
            {
                return RamDashDisplayPhase.Dashing;
            }

            if (cooldownRemaining > 0f)
            {
                return RamDashDisplayPhase.CooldownFilling;
            }

            return RamDashDisplayPhase.Ready;
        }
    }

    public float DisplayProgress
    {
        get
        {
            if (isCharging)
            {
                return chargeTime <= 0f ? 1f : Mathf.Clamp01(currentChargeTime / chargeTime);
            }

            if (cooldownRemaining <= 0f)
            {
                return 1f;
            }

            if (cooldown <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (cooldownRemaining / cooldown));
        }
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<ThirdPersonController>();
        playerHealth = GetComponent<PlayerHealth>();
        CachePlayerColliders();
        ResolveAudioSource();
        ResolvePlayerCameraController();
    }

    private void Reset()
    {
        ResolveAudioSource();
    }

    private void OnEnable()
    {
        chargeAction?.action?.Enable();
    }

    private void OnDisable()
    {
        CancelCharge();
        EndDash();
        StopInvulnerabilityCoroutine();
        chargeAction?.action?.Disable();
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPaused)
        {
            return;
        }

        UpdateCooldown(Time.deltaTime);
        UpdateCharge();
    }

    private void LateUpdate()
    {
        if (GameStateManager.Instance.IsPaused || !IsDashing)
        {
            return;
        }

        MoveDash(Time.deltaTime);
    }

    private void UpdateCharge()
    {
        if (!isCharging)
        {
            if (WasChargePressedThisFrame() && CanStartCharge())
            {
                StartCharge();
            }

            return;
        }

        if (requireGrounded && !characterController.isGrounded)
        {
            CancelCharge();
            return;
        }

        currentChargeTime = Mathf.Min(chargeTime, currentChargeTime + Time.deltaTime);

        if (!isFullyCharged && currentChargeTime >= chargeTime)
        {
            MarkFullyCharged();
        }

        if (!WasChargeReleasedThisFrame())
        {
            return;
        }

        if (isFullyCharged)
        {
            StartDash();
        }
        else
        {
            bool shouldPerformNormalDash = IsSharedInputMode;
            CancelCharge();

            if (shouldPerformNormalDash)
            {
                TryPerformNormalDashFallback();
            }
        }
    }

    public bool ShouldDeferNormalDashInput(InputAction normalDashAction)
    {
        return IsSharedWithNormalDash(normalDashAction) && CanStartCharge();
    }

    private bool CanStartCharge()
    {
        return cooldownRemaining <= 0f
            && !IsDashing
            && (!requireGrounded || characterController.isGrounded);
    }

    private void StartCharge()
    {
        isCharging = true;
        isFullyCharged = false;
        readySoundPlayed = false;
        currentChargeTime = chargeTime <= 0f ? chargeTime : 0f;
        chargeStarted?.Invoke();

        if (chargeTime <= 0f)
        {
            MarkFullyCharged();
        }
    }

    private void MarkFullyCharged()
    {
        isFullyCharged = true;
        currentChargeTime = chargeTime;

        if (!readySoundPlayed)
        {
            readySoundPlayed = true;
            PlaySound(readySound);
        }

        fullyCharged?.Invoke();
    }

    private void CancelCharge()
    {
        if (!isCharging)
        {
            return;
        }

        isCharging = false;
        isFullyCharged = false;
        readySoundPlayed = false;
        currentChargeTime = 0f;
        chargeCanceled?.Invoke();
    }

    private void StartDash()
    {
        isCharging = false;
        isFullyCharged = false;
        readySoundPlayed = false;
        currentChargeTime = 0f;

        dashDirection = GetFacingDirection();
        dashTimeRemaining = dashDuration;
        cooldownRemaining = cooldown;
        damagedEnemies.Clear();
        ignoredEnemies.Clear();
        RestoreIgnoredCollisions();

        ProcessDashContacts(Vector3.zero);
        GrantDashInvulnerability();
        ShakeCamera();
        PlaySound(ramDashStartSound);
        dashStarted?.Invoke();
    }

    private void MoveDash(float deltaTime)
    {
        float stepTime = Mathf.Min(deltaTime, dashTimeRemaining);
        Vector3 motion = dashDirection * dashSpeed * stepTime;

        ProcessDashContacts(motion);
        characterController.Move(motion);
        ProcessDashContacts(Vector3.zero);

        dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - deltaTime);
        if (dashTimeRemaining <= 0f)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        if (dashTimeRemaining <= 0f && ignoredCollisionPairs.Count == 0)
        {
            return;
        }

        dashTimeRemaining = 0f;
        RestoreIgnoredCollisions();
        dashEnded?.Invoke();
    }

    private void ProcessDashContacts(Vector3 motion)
    {
        if (motion.sqrMagnitude > 0f)
        {
            ProcessCapsuleCast(motion);
        }

        ProcessOverlapSphere(transform.position + hitOffset);
    }

    private void ProcessCapsuleCast(Vector3 motion)
    {
        GetDashCapsule(out Vector3 point1, out Vector3 point2, out float radius);

        float distance = motion.magnitude + hitRadius;
        Vector3 direction = motion.normalized;
        int hitCount = Physics.CapsuleCastNonAlloc(
            point1,
            point2,
            radius,
            direction,
            castResults,
            distance,
            enemyLayers,
            triggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = castResults[i].collider;
            TryHandleEnemyCollider(hitCollider);
        }
    }

    private void ProcessOverlapSphere(Vector3 center)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            hitRadius,
            overlapResults,
            enemyLayers,
            triggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            TryHandleEnemyCollider(overlapResults[i]);
        }
    }

    private void TryHandleEnemyCollider(Collider enemyCollider)
    {
        if (enemyCollider == null)
        {
            return;
        }

        EnemyHealth enemyHealth = enemyCollider.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null || enemyHealth.IsDead)
        {
            return;
        }

        IgnoreEnemyCollisions(enemyHealth);
        DamageEnemy(enemyHealth);
    }

    private void DamageEnemy(EnemyHealth enemyHealth)
    {
        if (!damagedEnemies.Add(enemyHealth))
        {
            return;
        }

        enemyHealth.TakeDamage(damage);
        PlaySound(ramDashHitEnemySound);
        enemyHit?.Invoke(enemyHealth);
    }

    private void IgnoreEnemyCollisions(EnemyHealth enemyHealth)
    {
        if (!ignoreEnemyCollisionsDuringDash)
        {
            return;
        }

        if (!ignoredEnemies.Add(enemyHealth))
        {
            return;
        }

        Collider[] enemyColliders = enemyHealth.GetComponentsInChildren<Collider>();
        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == null)
            {
                continue;
            }

            foreach (Collider enemyCollider in enemyColliders)
            {
                if (enemyCollider == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(playerCollider, enemyCollider, true);
                ignoredCollisionPairs.Add(new IgnoredCollisionPair(playerCollider, enemyCollider));
            }
        }
    }

    private void RestoreIgnoredCollisions()
    {
        foreach (IgnoredCollisionPair collisionPair in ignoredCollisionPairs)
        {
            if (collisionPair.PlayerCollider != null && collisionPair.EnemyCollider != null)
            {
                Physics.IgnoreCollision(collisionPair.PlayerCollider, collisionPair.EnemyCollider, false);
            }
        }

        ignoredCollisionPairs.Clear();
        ignoredEnemies.Clear();
    }

    private Vector3 GetFacingDirection()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
        {
            return Vector3.forward;
        }

        return forward.normalized;
    }

    private void GetDashCapsule(out Vector3 point1, out Vector3 point2, out float radius)
    {
        radius = Mathf.Max(characterController.radius, hitRadius);
        float halfHeight = Mathf.Max(characterController.height * 0.5f - radius, 0f);
        Vector3 center = transform.TransformPoint(characterController.center);
        point1 = center + (Vector3.up * halfHeight);
        point2 = center - (Vector3.up * halfHeight);
    }

    private void UpdateCooldown(float deltaTime)
    {
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
        }

    }

    private void TryPerformNormalDashFallback()
    {
        ResolvePlayerController();
        playerController?.TryStartDashFromCurrentInput();
    }

    private bool IsSharedWithNormalDash(InputAction normalDashAction)
    {
        InputAction ramDashAction = chargeAction != null ? chargeAction.action : null;
        if (ramDashAction == null || normalDashAction == null)
        {
            return false;
        }

        if (ramDashAction == normalDashAction || ramDashAction.id == normalDashAction.id)
        {
            return true;
        }

        return ActionsShareAnyBinding(ramDashAction, normalDashAction);
    }

    private bool ActionsShareAnyBinding(InputAction firstAction, InputAction secondAction)
    {
        foreach (InputBinding firstBinding in firstAction.bindings)
        {
            string firstPath = GetEffectiveBindingPath(firstBinding);
            if (string.IsNullOrEmpty(firstPath))
            {
                continue;
            }

            foreach (InputBinding secondBinding in secondAction.bindings)
            {
                string secondPath = GetEffectiveBindingPath(secondBinding);
                if (!string.IsNullOrEmpty(secondPath) && firstPath == secondPath)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetEffectiveBindingPath(InputBinding binding)
    {
        if (binding.isComposite || binding.isPartOfComposite)
        {
            return string.Empty;
        }

        return !string.IsNullOrEmpty(binding.effectivePath) ? binding.effectivePath : binding.path;
    }

    private void GrantDashInvulnerability()
    {
        if (iframeDuration <= 0f)
        {
            return;
        }

        StopInvulnerabilityCoroutine();

        if (iframeStartDelay <= 0f)
        {
            ResolvePlayerHealth();
            playerHealth?.GrantInvulnerability(iframeDuration);
            return;
        }

        invulnerabilityCoroutine = StartCoroutine(GrantInvulnerabilityAfterDelay());
    }

    private IEnumerator GrantInvulnerabilityAfterDelay()
    {
        yield return new WaitForSeconds(iframeStartDelay);
        ResolvePlayerHealth();
        playerHealth?.GrantInvulnerability(iframeDuration);
        invulnerabilityCoroutine = null;
    }

    private void StopInvulnerabilityCoroutine()
    {
        if (invulnerabilityCoroutine == null)
        {
            return;
        }

        StopCoroutine(invulnerabilityCoroutine);
        invulnerabilityCoroutine = null;
    }

    private void ShakeCamera()
    {
        if (cameraShakeDuration <= 0f || cameraShakeIntensity <= 0f)
        {
            return;
        }

        ResolvePlayerCameraController();
        playerCameraController?.Shake(cameraShakeDuration, cameraShakeIntensity);
    }

    private bool WasChargePressedThisFrame()
    {
        if (chargeAction != null && chargeAction.action != null)
        {
            return chargeAction.action.WasPressedThisFrame();
        }

        return Keyboard.current != null
            && Keyboard.current[keyboardFallbackKey].wasPressedThisFrame;
    }

    private bool WasChargeReleasedThisFrame()
    {
        if (chargeAction != null && chargeAction.action != null)
        {
            return chargeAction.action.WasReleasedThisFrame();
        }

        return Keyboard.current != null
            && Keyboard.current[keyboardFallbackKey].wasReleasedThisFrame;
    }

    private void CachePlayerColliders()
    {
        playerColliders = GetComponentsInChildren<Collider>();
    }

    private void ResolveAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void ResolvePlayerController()
    {
        if (playerController == null)
        {
            playerController = GetComponent<ThirdPersonController>();
        }
    }

    private void ResolvePlayerHealth()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void ResolvePlayerCameraController()
    {
        if (playerCameraController != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerCameraController = mainCamera.GetComponent<PlayerCameraController>();
        }

        if (playerCameraController == null)
        {
            playerCameraController = FindAnyObjectByType<PlayerCameraController>();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        ResolveAudioSource();

        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void OnValidate()
    {
        chargeTime = Mathf.Max(0f, chargeTime);
        dashSpeed = Mathf.Max(0.01f, dashSpeed);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        damage = Mathf.Max(1, damage);
        cooldown = Mathf.Max(0f, cooldown);
        hitRadius = Mathf.Max(0.01f, hitRadius);
        iframeStartDelay = Mathf.Max(0f, iframeStartDelay);
        iframeDuration = Mathf.Max(0f, iframeDuration);
        cameraShakeIntensity = Mathf.Max(0f, cameraShakeIntensity);
        cameraShakeDuration = Mathf.Max(0f, cameraShakeDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + hitOffset, hitRadius);
    }
}
