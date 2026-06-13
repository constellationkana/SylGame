using TMPro;
using UnityEngine;

/// <summary>
/// Updates simple on-screen HUD text for health, collectible count, and objective status.
/// </summary>
public class SimpleHUD : MonoBehaviour
{
    [Header("Text Labels")]
    [SerializeField] private TMP_Text collectiblesText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Scene References")]
    [SerializeField] private CollectibleManager collectibleManager;
    [SerializeField] private CollectibleObjective collectibleObjective;
    [SerializeField] private PlayerHealth playerHealth;

    private bool isSubscribedToCollectibles;
    private bool isSubscribedToObjective;
    private bool isSubscribedToHealth;

    private void Awake()
    {
        SetGameOverPanelVisible(false);
    }

    private void OnEnable()
    {
        ConnectToSceneSystems(false);
        Refresh();
    }

    private void Start()
    {
        ConnectToSceneSystems(true);
        WarnAboutMissingTextReferences();
        Refresh();
    }

    private void OnDisable()
    {
        if (collectibleManager != null && isSubscribedToCollectibles)
        {
            collectibleManager.CollectibleCollected -= HandleCollectibleCollected;
            isSubscribedToCollectibles = false;
        }

        if (collectibleObjective != null && isSubscribedToObjective)
        {
            collectibleObjective.ObjectiveProgressChanged -= HandleObjectiveProgressChanged;
            collectibleObjective.ObjectiveCompleted -= HandleObjectiveCompleted;
            isSubscribedToObjective = false;
        }

        if (playerHealth != null && isSubscribedToHealth)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
            isSubscribedToHealth = false;
        }
    }

    /// <summary>
    /// Refreshes both HUD labels from the current collectible and objective state.
    /// </summary>
    public void Refresh()
    {
        RefreshCollectibles();
        RefreshObjective();
        RefreshGameOverPanel();
    }

    private void ConnectToSceneSystems(bool showWarnings)
    {
        if (collectibleManager == null)
        {
            collectibleManager = CollectibleManager.Instance;
        }

        if (collectibleManager == null)
        {
            collectibleManager = FindAnyObjectByType<CollectibleManager>();
        }

        if (collectibleObjective == null)
        {
            collectibleObjective = FindAnyObjectByType<CollectibleObjective>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
        }

        if (collectibleManager != null && !isSubscribedToCollectibles)
        {
            collectibleManager.CollectibleCollected += HandleCollectibleCollected;
            isSubscribedToCollectibles = true;
        }
        else if (showWarnings && collectibleManager == null)
        {
            Debug.LogWarning($"{nameof(SimpleHUD)} could not find a {nameof(CollectibleManager)} in the scene.", this);
        }

        if (collectibleObjective != null && !isSubscribedToObjective)
        {
            collectibleObjective.ObjectiveProgressChanged += HandleObjectiveProgressChanged;
            collectibleObjective.ObjectiveCompleted += HandleObjectiveCompleted;
            isSubscribedToObjective = true;
        }
        else if (showWarnings && collectibleObjective == null)
        {
            Debug.LogWarning($"{nameof(SimpleHUD)} could not find a {nameof(CollectibleObjective)} in the scene.", this);
        }

        if (playerHealth != null && !isSubscribedToHealth)
        {
            playerHealth.HealthChanged += HandleHealthChanged;
            isSubscribedToHealth = true;
        }
        else if (showWarnings && playerHealth == null)
        {
            Debug.LogWarning($"{nameof(SimpleHUD)} could not find a {nameof(PlayerHealth)} in the scene.", this);
        }
    }

    private void WarnAboutMissingTextReferences()
    {
        if (collectiblesText == null)
        {
            Debug.LogWarning($"{nameof(SimpleHUD)} needs a TextMeshPro label for collectibles.", this);
        }

        if (objectiveText == null)
        {
            Debug.LogWarning($"{nameof(SimpleHUD)} needs a TextMeshPro label for objective status.", this);
        }
    }

    private void RefreshCollectibles()
    {
        int collectedCount = collectibleManager != null ? collectibleManager.CollectedCount : 0;
        UpdateCollectiblesText(collectedCount);
    }

    private void RefreshObjective()
    {
        if (collectibleObjective == null)
        {
            UpdateObjectiveText(0, 0, false);
            return;
        }

        UpdateObjectiveText(
            collectibleObjective.CurrentProgress,
            collectibleObjective.TargetCollectibleCount,
            collectibleObjective.IsComplete);
    }

    private void HandleCollectibleCollected(int collectedCount)
    {
        UpdateCollectiblesText(collectedCount);
    }

    private void HandleObjectiveProgressChanged(int currentProgress, int targetCount)
    {
        bool isComplete = collectibleObjective != null && collectibleObjective.IsComplete;
        UpdateObjectiveText(currentProgress, targetCount, isComplete);
    }

    private void HandleObjectiveCompleted()
    {
        RefreshObjective();
    }

    private void HandleHealthChanged(int currentHealth)
    {
        RefreshObjective();
        SetGameOverPanelVisible(currentHealth <= 0);
    }

    private void RefreshGameOverPanel()
    {
        if (playerHealth == null)
        {
            SetGameOverPanelVisible(false);
            return;
        }

        SetGameOverPanelVisible(playerHealth.CurrentHealth <= 0);
    }

    private void SetGameOverPanelVisible(bool isVisible)
    {
        if (gameOverPanel == null)
        {
            return;
        }

        gameOverPanel.SetActive(isVisible);
    }

    private void UpdateCollectiblesText(int collectedCount)
    {
        if (collectiblesText == null)
        {
            return;
        }

        collectiblesText.text = $"Collectibles: {collectedCount}";
    }

    private void UpdateObjectiveText(int currentProgress, int targetCount, bool isComplete)
    {
        if (objectiveText == null)
        {
            return;
        }

        if (isComplete)
        {
            objectiveText.text = $"{GetHealthText()}\nObjective: Complete";
            return;
        }

        if (targetCount <= 0)
        {
            objectiveText.text = $"{GetHealthText()}\nObjective: Not assigned";
            return;
        }

        objectiveText.text = $"{GetHealthText()}\nObjective: {currentProgress}/{targetCount}";
    }

    private string GetHealthText()
    {
        int currentHealth = playerHealth != null ? playerHealth.CurrentHealth : 0;
        return $"Health: {currentHealth}";
    }
}
