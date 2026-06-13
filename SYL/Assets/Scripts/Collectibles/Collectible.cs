using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private bool requirePlayerTag;
    [SerializeField] private string playerTag = "Player";

    private bool isCollected;

    private void Reset()
    {
        Collider collectibleCollider = GetComponent<Collider>();
        collectibleCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider collectibleCollider = GetComponent<Collider>();
        if (collectibleCollider != null && !collectibleCollider.isTrigger)
        {
            collectibleCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || !IsPlayer(other))
        {
            return;
        }

        Collect();
    }

    private void Collect()
    {
        isCollected = true;

        Debug.Log($"Collected {gameObject.name}.", this);

        if (CollectibleManager.Instance != null)
        {
            CollectibleManager.Instance.RegisterCollection(this);
        }
        else
        {
            Debug.LogWarning($"{nameof(Collectible)} on {gameObject.name} was collected, but no {nameof(CollectibleManager)} exists in the scene.", this);
        }

        if (destroyOnCollect)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other.GetComponentInParent<ThirdPersonController>() != null ||
            other.GetComponentInParent<PlayerInteraction>() != null)
        {
            return !requirePlayerTag || other.CompareTag(playerTag);
        }

        return requirePlayerTag && other.CompareTag(playerTag);
    }
}
