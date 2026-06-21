using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float rotationSmoothTime = 0.12f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.75f;

    [Header("Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Input Actions")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string dashActionName = "Dash";

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private RamDashAbility ramDashAbility;

    private float verticalVelocity;
    private float currentRotationVelocity;
    private float dashTimeRemaining;
    private float dashCooldownRemaining;
    private Vector3 dashDirection;
    private bool firstPersonCameraActive;

    public float DashCooldownProgress
    {
        get
        {
            if (dashCooldown <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (dashCooldownRemaining / dashCooldown));
        }
    }

    public InputAction DashInputAction
    {
        get { return dashAction; }
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        ramDashAbility = GetComponent<RamDashAbility>();
        ResolveCameraTransform();

        if (playerInput.actions == null)
        {
            Debug.LogError(
                $"{nameof(ThirdPersonController)} needs a PlayerInput actions asset. Assign Assets/InputSystem_Actions.inputactions to the PlayerInput component.",
                this);
            enabled = false;
            return;
        }

        moveAction = playerInput.actions.FindAction(moveActionName);
        sprintAction = playerInput.actions.FindAction(sprintActionName);
        jumpAction = playerInput.actions.FindAction(jumpActionName);
        dashAction = playerInput.actions.FindAction(dashActionName);

        if (moveAction == null || sprintAction == null || jumpAction == null || dashAction == null)
        {
            Debug.LogError(
                $"{nameof(ThirdPersonController)} could not find '{moveActionName}', '{sprintActionName}', '{jumpActionName}', and/or '{dashActionName}' in the PlayerInput actions asset.",
                this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        sprintAction?.Enable();
        jumpAction?.Enable();
        dashAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        sprintAction?.Disable();
        jumpAction?.Disable();
        dashAction?.Disable();
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPaused)
        {
            return;
        }

        if (moveAction == null || sprintAction == null || jumpAction == null || dashAction == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        UpdateDashCooldown(deltaTime);

        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        if (firstPersonCameraActive)
        {
            RotateWithCameraYaw();
        }

        Vector3 moveDirection = GetCameraRelativeMoveDirection(moveInput);

        if (!firstPersonCameraActive)
        {
            RotateTowardMovement(moveDirection);
        }

        TryStartDash(moveDirection);
        ApplyJumpAndGravity();

        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        Vector3 velocity = GetHorizontalVelocity(moveDirection, speed);
        velocity.y = verticalVelocity;

        characterController.Move(velocity * deltaTime);
        UpdateDashTimer(deltaTime);
    }

    private Vector3 GetCameraRelativeMoveDirection(Vector2 moveInput)
    {
        ResolveCameraTransform();

        Transform movementBasis = cameraTransform != null ? cameraTransform : transform;
        Vector3 forward = movementBasis.forward;
        Vector3 right = movementBasis.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 direction = (forward * moveInput.y) + (right * moveInput.x);
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private void RotateTowardMovement(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
        float smoothedAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            targetAngle,
            ref currentRotationVelocity,
            rotationSmoothTime);

        transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
    }

    private void RotateWithCameraYaw()
    {
        ResolveCameraTransform();

        if (cameraTransform == null)
        {
            return;
        }

        transform.rotation = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);
    }

    private void ApplyJumpAndGravity()
    {
        bool isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
        }

        if (isGrounded && jumpAction.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else if (!isGrounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void TryStartDash(Vector3 moveDirection)
    {
        if (!dashAction.WasPressedThisFrame())
        {
            return;
        }

        if (ramDashAbility != null && ramDashAbility.ShouldDeferNormalDashInput(dashAction))
        {
            return;
        }

        StartDash(moveDirection);
    }

    public bool TryStartDashFromCurrentInput()
    {
        if (moveAction == null)
        {
            return StartDash(Vector3.zero);
        }

        return StartDash(GetCameraRelativeMoveDirection(moveAction.ReadValue<Vector2>()));
    }

    private bool StartDash(Vector3 moveDirection)
    {
        if (!CanDash())
        {
            return false;
        }

        dashDirection = moveDirection.sqrMagnitude > 0.01f
            ? moveDirection.normalized
            : GetForwardDashDirection();

        dashTimeRemaining = dashDuration;
        dashCooldownRemaining = dashCooldown;
        return true;
    }

    private bool CanDash()
    {
        return dashTimeRemaining <= 0f
            && dashCooldownRemaining <= 0f
            && characterController.isGrounded;
    }

    private Vector3 GetHorizontalVelocity(Vector3 moveDirection, float speed)
    {
        if (dashTimeRemaining > 0f)
        {
            return dashDirection * (dashDistance / dashDuration);
        }

        return moveDirection * speed;
    }

    private Vector3 GetForwardDashDirection()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
        {
            return Vector3.forward;
        }

        return forward.normalized;
    }

    private void UpdateDashCooldown(float deltaTime)
    {
        if (dashCooldownRemaining > 0f)
        {
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - deltaTime);
        }
    }

    private void UpdateDashTimer(float deltaTime)
    {
        if (dashTimeRemaining > 0f)
        {
            dashTimeRemaining = Mathf.Max(0f, dashTimeRemaining - deltaTime);
        }
    }

    public void SetFirstPersonCameraActive(bool isActive)
    {
        firstPersonCameraActive = isActive;
    }

    public void SetCameraTransform(Transform newCameraTransform)
    {
        if (newCameraTransform != null)
        {
            cameraTransform = newCameraTransform;
        }
    }

    private void ResolveCameraTransform()
    {
        if (cameraTransform != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
    }

    private void OnValidate()
    {
        dashDistance = Mathf.Max(0f, dashDistance);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
    }
}
