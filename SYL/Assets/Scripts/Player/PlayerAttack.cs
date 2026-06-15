using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private Camera playerCamera = null;
    [SerializeField] private PlayerCameraController playerCameraController = null;
    [SerializeField] private Transform interactionOrigin = null;
    [SerializeField] private LayerMask attackLayers = Physics.DefaultRaycastLayers;
    [Min(0f)]
    [SerializeField] private float attackRange = 2f;
    [Min(0f)]
    [SerializeField] private float attackRadius = 0.35f;
    [Min(1)]
    [SerializeField] private int attackDamage = 1;

    private void Awake()
    {
        ResolvePlayerCamera();
        ResolvePlayerCameraController();
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Attack();
    }

    private void Attack()
    {
        ResolvePlayerCamera();
        ResolvePlayerCameraController();

        if (playerCamera == null)
        {
            Debug.LogWarning($"{nameof(PlayerAttack)} could not attack because no player camera was found.", this);
            return;
        }

        Ray ray = new Ray(GetAttackOriginPosition(), GetAttackDirection());

        if (!Physics.SphereCast(ray, attackRadius, out RaycastHit hit, attackRange, attackLayers))
        {
            return;
        }

        EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.TakeDamage(attackDamage);
    }

    private Vector3 GetAttackOriginPosition()
    {
        if (interactionOrigin != null)
        {
            return interactionOrigin.position;
        }

        if (playerCamera != null)
        {
            return playerCamera.transform.position;
        }

        return transform.position;
    }

    private Vector3 GetAttackDirection()
    {
        if (playerCameraController != null && playerCameraController.IsFirstPerson)
        {
            return playerCamera.transform.forward;
        }

        return transform.forward;
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

    private void ResolvePlayerCameraController()
    {
        if (playerCameraController != null || playerCamera == null)
        {
            return;
        }

        playerCameraController = playerCamera.GetComponent<PlayerCameraController>();
    }

    private void OnValidate()
    {
        attackRange = Mathf.Max(0f, attackRange);
        attackRadius = Mathf.Max(0f, attackRadius);
        attackDamage = Mathf.Max(1, attackDamage);
    }
}
