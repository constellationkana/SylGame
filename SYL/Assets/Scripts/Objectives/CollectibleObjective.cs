using UnityEngine;

public class CollectibleObjective : MonoBehaviour
{
    [SerializeField] private int targetCollectibleCount = 3;

    private CollectibleManager collectibleManager;
    private bool isComplete;

    private void OnEnable()
    {
        TrySubscribeToCollectibleManager(false);
    }

    private void Start()
    {
        TrySubscribeToCollectibleManager(true);
    }

    private void OnDisable()
    {
        if (collectibleManager != null)
        {
            collectibleManager.CollectibleCollected -= HandleCollectibleCollected;
            collectibleManager = null;
        }
    }

    private void OnValidate()
    {
        if (targetCollectibleCount < 1)
        {
            targetCollectibleCount = 1;
        }
    }

    private void TrySubscribeToCollectibleManager(bool showWarning)
    {
        if (collectibleManager != null)
        {
            return;
        }

        collectibleManager = CollectibleManager.Instance;
        if (collectibleManager != null)
        {
            collectibleManager.CollectibleCollected += HandleCollectibleCollected;
        }
        else if (showWarning)
        {
            Debug.LogWarning($"{nameof(CollectibleObjective)} could not find a {nameof(CollectibleManager)} in the scene.", this);
        }
    }

    private void HandleCollectibleCollected(int collectedCount)
    {
        int displayedProgress = Mathf.Min(collectedCount, targetCollectibleCount);
        Debug.Log($"Objective Progress: {displayedProgress}/{targetCollectibleCount}", this);

        if (!isComplete && collectedCount >= targetCollectibleCount)
        {
            isComplete = true;
            Debug.Log("Objective Complete", this);
        }
    }
}
