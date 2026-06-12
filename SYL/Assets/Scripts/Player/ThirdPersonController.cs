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
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Input Actions")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string sprintActionName = "Sprint";

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;

    private float verticalVelocity;
    private float currentRotationVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

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

        if (moveAction == null || sprintAction == null)
        {
            Debug.LogError(
                $"{nameof(ThirdPersonController)} could not find '{moveActionName}' and/or '{sprintActionName}' in the PlayerInput actions asset.",
                this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        sprintAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        sprintAction?.Disable();
    }

    private void Update()
    {
        if (moveAction == null || sprintAction == null)
        {
            return;
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = GetCameraRelativeMoveDirection(moveInput);

        RotateTowardMovement(moveDirection);
        ApplyGravity();

        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private Vector3 GetCameraRelativeMoveDirection(Vector2 moveInput)
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            right = cameraTransform.right;
        }

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

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedGravity;
            return;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }
}
