using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private PlayerInput playerInput;

    [Header("Input Actions")]
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string toggleCameraActionName = "ToggleCamera";

    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float followDistance = 5f;
    [SerializeField] private float heightOffset = 1.6f;
    [SerializeField] private bool startInFirstPerson;
    [SerializeField] private bool lockCursorOnStart = true;

    [Header("Pitch Limits")]
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 75f;

    private InputAction lookAction;
    private InputAction toggleCameraAction;
    private bool isFirstPerson;
    private float yaw;
    private float pitch = 15f;

    public bool IsFirstPerson => isFirstPerson;

    private void Awake()
    {
        yaw = transform.eulerAngles.y;
        pitch = NormalizePitch(transform.eulerAngles.x);
        isFirstPerson = startInFirstPerson;

        if (playerInput != null && playerInput.actions != null)
        {
            lookAction = playerInput.actions.FindAction(lookActionName);
            toggleCameraAction = playerInput.actions.FindAction(toggleCameraActionName);
        }

        ApplyPerspective();
    }

    private void OnEnable()
    {
        lookAction?.Enable();
        toggleCameraAction?.Enable();

        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        lookAction?.Disable();
        toggleCameraAction?.Disable();
    }

    private void Update()
    {
        ReadLookInput();

        if (WasTogglePressed())
        {
            isFirstPerson = !isFirstPerson;
            ApplyPerspective();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + (Vector3.up * heightOffset);
        Vector3 cameraPosition = isFirstPerson
            ? focusPoint
            : focusPoint - (cameraRotation * Vector3.forward * followDistance);

        transform.SetPositionAndRotation(cameraPosition, cameraRotation);
    }

    private void ReadLookInput()
    {
        Vector2 lookDelta = lookAction != null ? lookAction.ReadValue<Vector2>() : ReadFallbackLookInput();
        if (lookDelta.sqrMagnitude < 0.001f)
        {
            return;
        }

        bool usingMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.001f;
        float inputScale = usingMouse ? 0.1f : 60f * Time.deltaTime;

        yaw += lookDelta.x * mouseSensitivity * inputScale;
        pitch -= lookDelta.y * mouseSensitivity * inputScale;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private Vector2 ReadFallbackLookInput()
    {
        Vector2 lookDelta = Vector2.zero;

        if (Mouse.current != null)
        {
            lookDelta += Mouse.current.delta.ReadValue();
        }

        if (Gamepad.current != null)
        {
            lookDelta += Gamepad.current.rightStick.ReadValue();
        }

        return lookDelta;
    }

    private bool WasTogglePressed()
    {
        if (toggleCameraAction != null && toggleCameraAction.WasPressedThisFrame())
        {
            return true;
        }

        return Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
    }

    private void ApplyPerspective()
    {
        if (playerController != null)
        {
            playerController.SetFirstPersonCameraActive(isFirstPerson);
        }
    }

    private float NormalizePitch(float rawPitch)
    {
        return rawPitch > 180f ? rawPitch - 360f : rawPitch;
    }
}
