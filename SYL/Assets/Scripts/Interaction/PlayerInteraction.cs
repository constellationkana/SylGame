using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private Transform interactionOrigin;

    private void Awake()
    {
        ResolvePlayerCamera();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        TryInteract();
    }

    private void TryInteract()
    {
        ResolvePlayerCamera();

        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(
            interactionOrigin.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayers))
        {
            return;
        }

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable == null)
        {
            return;
        }

        Debug.Log($"Interacted with {hit.collider.gameObject.name}.", hit.collider);
        interactable.Interact();
    }

    private void ResolvePlayerCamera()
    {
        if (playerCamera != null)
        {
            return;
        }

        playerCamera = GetComponent<Camera>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }
}
