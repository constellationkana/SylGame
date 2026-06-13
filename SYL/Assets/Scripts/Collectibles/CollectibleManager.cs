using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    public event System.Action<int> CollectibleCollected;

    public int CollectedCount { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(CollectibleManager)} found on {gameObject.name}. Disabling this instance.", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterCollection(Collectible collectible)
    {
        CollectedCount++;
        Debug.Log($"Collectibles collected: {CollectedCount}.", collectible);
        CollectibleCollected?.Invoke(CollectedCount);
    }
}
