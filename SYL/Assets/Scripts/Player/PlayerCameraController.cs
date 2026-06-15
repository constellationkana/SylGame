using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Transform firstPersonVisibilityRoot;

    [Header("Input Actions")]
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string toggleCameraActionName = "ToggleCamera";

    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 1.5f;
    [SerializeField] private float followDistance = 5f;
    [Tooltip("Camera height while the player is looking through the character's eyes.")]
    [SerializeField] private float firstPersonHeight = 1.7f;
    [Tooltip("Camera focus height while following the full character from behind.")]
    [SerializeField] private float thirdPersonHeight = 1.7f;
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
    private float shakeTimer;
    private float shakeDuration;
    private float shakeMagnitude;
    private FirstPersonHiddenVisual[] firstPersonHiddenVisuals;

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

        CacheFirstPersonHiddenVisuals();
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
        // Each perspective gets its own height so future tuning does not affect both modes.
        Vector3 focusPoint = target.position + (Vector3.up * GetCurrentCameraHeight());
        Vector3 cameraPosition = isFirstPerson
            ? focusPoint
            : focusPoint - (cameraRotation * Vector3.forward * followDistance);

        cameraPosition += GetShakeOffset(cameraRotation);
        transform.SetPositionAndRotation(cameraPosition, cameraRotation);
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = Mathf.Max(0f, duration);
        shakeTimer = shakeDuration;
        shakeMagnitude = Mathf.Max(0f, magnitude);
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
        CacheFirstPersonHiddenVisuals();

        if (playerController != null)
        {
            playerController.SetCameraTransform(transform);
            playerController.SetFirstPersonCameraActive(isFirstPerson);
        }

        foreach (FirstPersonHiddenVisual hiddenVisual in firstPersonHiddenVisuals)
        {
            if (hiddenVisual != null)
            {
                hiddenVisual.SetHiddenForFirstPerson(isFirstPerson);
            }
        }
    }

    private void CacheFirstPersonHiddenVisuals()
    {
        Transform searchRoot = firstPersonVisibilityRoot != null ? firstPersonVisibilityRoot : target;
        if (searchRoot == null)
        {
            firstPersonHiddenVisuals = new FirstPersonHiddenVisual[0];
            return;
        }

        firstPersonHiddenVisuals = searchRoot.GetComponentsInChildren<FirstPersonHiddenVisual>(true);
    }

    private float GetCurrentCameraHeight()
    {
        return isFirstPerson ? firstPersonHeight : thirdPersonHeight;
    }

    private Vector3 GetShakeOffset(Quaternion cameraRotation)
    {
        if (shakeTimer <= 0f || shakeMagnitude <= 0f)
        {
            return Vector3.zero;
        }

        shakeTimer = Mathf.Max(0f, shakeTimer - Time.deltaTime);

        if (shakeTimer <= 0f)
        {
            return Vector3.zero;
        }

        float shakeStrength = shakeMagnitude * (shakeTimer / shakeDuration);
        Vector2 randomOffset = Random.insideUnitCircle * shakeStrength;

        return cameraRotation * new Vector3(randomOffset.x, randomOffset.y, 0f);
    }

    private float NormalizePitch(float rawPitch)
    {
        return rawPitch > 180f ? rawPitch - 360f : rawPitch;
    }
}
