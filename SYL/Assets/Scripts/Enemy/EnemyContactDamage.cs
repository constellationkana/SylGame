using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyContactDamage : MonoBehaviour
{
    [Header("Damage")]
    [Min(1)]
    [SerializeField] private int damageAmount = 1;

    private readonly HashSet<PlayerHealth> playersTouching = new HashSet<PlayerHealth>();

    private void Awake()
    {
        Rigidbody enemyRigidbody = GetComponent<Rigidbody>();

        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamagePlayer(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        StopTouchingPlayer(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamagePlayer(other);
    }

    private void OnTriggerExit(Collider other)
    {
        StopTouchingPlayer(other);
    }

    private void TryDamagePlayer(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playersTouching.Contains(playerHealth))
        {
            return;
        }

        playersTouching.Add(playerHealth);
        playerHealth.TakeDamage(damageAmount);
    }

    private void StopTouchingPlayer(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playersTouching.Remove(playerHealth);
        }
    }

    private void OnValidate()
    {
        damageAmount = Mathf.Max(1, damageAmount);
    }
}
