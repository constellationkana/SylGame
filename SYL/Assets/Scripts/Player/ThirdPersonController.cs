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

    [Header("Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Input Actions")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string jumpActionName = "Jump";

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;

    private float verticalVelocity;
    private float currentRotationVelocity;
    private bool firstPersonCameraActive;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
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

        if (moveAction == null || sprintAction == null || jumpAction == null)
        {
            Debug.LogError(
                $"{nameof(ThirdPersonController)} could not find '{moveActionName}', '{sprintActionName}', and/or '{jumpActionName}' in the PlayerInput actions asset.",
                this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        sprintAction?.Enable();
        jumpAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        sprintAction?.Disable();
        jumpAction?.Disable();
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPaused)
        {
            return;
        }

        if (moveAction == null || sprintAction == null || jumpAction == null)
        {
            return;
        }

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

        ApplyJumpAndGravity();

        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
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
}
