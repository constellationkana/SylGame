using UnityEngine;

public class CollectibleObjective : MonoBehaviour
{
    [SerializeField] private int targetCollectibleCount = 3;

    private CollectibleManager collectibleManager;
    private int currentProgress;
    private bool isComplete;

    /// <summary>
    /// Raised whenever the collectible objective progress changes. Sends current progress and target count.
    /// </summary>
    public event System.Action<int, int> ObjectiveProgressChanged;

    /// <summary>
    /// Raised once when the collectible objective reaches its target count.
    /// </summary>
    public event System.Action ObjectiveCompleted;

    /// <summary>
    /// Gets the number of collectibles currently counted toward this objective.
    /// </summary>
    public int CurrentProgress => currentProgress;

    /// <summary>
    /// Gets the number of collectibles required to complete this objective.
    /// </summary>
    public int TargetCollectibleCount => targetCollectibleCount;

    /// <summary>
    /// Gets whether this objective has been completed.
    /// </summary>
    public bool IsComplete => isComplete;

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
            UpdateProgress(collectibleManager.CollectedCount);
        }
        else if (showWarning)
        {
            Debug.LogWarning($"{nameof(CollectibleObjective)} could not find a {nameof(CollectibleManager)} in the scene.", this);
        }
    }

    private void HandleCollectibleCollected(int collectedCount)
    {
        UpdateProgress(collectedCount);
    }

    private void UpdateProgress(int collectedCount)
    {
        int displayedProgress = Mathf.Min(collectedCount, targetCollectibleCount);

        if (currentProgress != displayedProgress)
        {
            currentProgress = displayedProgress;
            ObjectiveProgressChanged?.Invoke(currentProgress, targetCollectibleCount);
        }

        if (!isComplete && collectedCount >= targetCollectibleCount)
        {
            isComplete = true;
            ObjectiveCompleted?.Invoke();
        }
    }
}
