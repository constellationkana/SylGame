using UnityEngine;

/// <summary>
/// Moves a simple enemy projectile and damages the player on contact.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private float destroyTime;
    private Transform ownerRoot;

    public void Initialize(Vector3 travelDirection, float travelSpeed, int damageAmount, float lifetime, Transform owner)
    {
        direction = travelDirection.sqrMagnitude > 0.001f ? travelDirection.normalized : Vector3.forward;
        speed = Mathf.Max(0.01f, travelSpeed);
        damage = Mathf.Max(1, damageAmount);
        destroyTime = Time.time + Mathf.Max(0.01f, lifetime);
        ownerRoot = owner;
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPaused)
        {
            return;
        }

        transform.position += direction * speed * Time.deltaTime;

        if (Time.time >= destroyTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ownerRoot != null && other.transform.IsChildOf(ownerRoot))
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
