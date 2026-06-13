using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("The player Transform this enemy checks distance against.")]
    [SerializeField] private Transform player;
    [Tooltip("How close the player must be before this enemy reports detection.")]
    [Min(0f)]
    [SerializeField] private float detectionRadius = 5f;

    private bool playerDetected;

    private void Update()
    {
        if (player == null)
        {
            if (playerDetected)
            {
                playerDetected = false;
                Debug.Log("Player lost.", this);
            }

            return;
        }

        bool playerIsInRange = Vector3.Distance(transform.position, player.position) <= detectionRadius;

        if (playerIsInRange == playerDetected)
        {
            return;
        }

        playerDetected = playerIsInRange;

        if (playerDetected)
        {
            Debug.Log("Player detected!", this);
        }
        else
        {
            Debug.Log("Player lost.", this);
        }
    }

    private void OnValidate()
    {
        detectionRadius = Mathf.Max(0f, detectionRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
