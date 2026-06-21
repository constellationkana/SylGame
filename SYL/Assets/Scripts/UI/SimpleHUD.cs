using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Updates simple on-screen HUD text for health, collectible count, and objective status.
/// </summary>
public class SimpleHUD : MonoBehaviour
{
    [Header("Text Labels")]
    [SerializeField] private TMP_Text collectiblesText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Dash Cooldown")]
    [SerializeField] private Slider dashCooldownSlider;
    [SerializeField] private Image dashCooldownFill;
    [SerializeField] private Color dashCooldownReadyColor = Color.green;
    [SerializeField] private Color dashCooldownEmptyColor = Color.red;

    [Header("TV Remote Cooldown")]
    [SerializeField] private Slider tvRemoteCooldownSlider;
    [SerializeField] private Image tvRemoteCooldownFill;
    [SerializeField] private Color tvRemoteCooldownReadyColor = Color.blue;
    [SerializeField] private Color tvRemoteCooldownEmptyColor = Color.red;

    [Header("Scene References")]
    [SerializeField] private CollectibleManager collectibleManager;
    [SerializeField] private CollectibleObjective collectibleObjective;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private TVRemoteAbility tvRemoteAbility;

    private bool isSubscribedToCollectibles;
    private bool isSubscribedToObjective;
    private bool isSubscribedToHealth;

    private void Awake()
    {
        SetGameOverPanelVisible(false);
        EnsureDashCooldownSlider();
        EnsureTVRemoteCooldownSlider();
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

    private void Update()
    {
        RefreshDashCooldown();
        RefreshTVRemoteCooldown();
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
        RefreshDashCooldown();
        RefreshTVRemoteCooldown();
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

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<ThirdPersonController>();
        }

        if (tvRemoteAbility == null)
        {
            tvRemoteAbility = FindAnyObjectByType<TVRemoteAbility>();
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

    private void RefreshDashCooldown()
    {
        if (dashCooldownSlider == null)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<ThirdPersonController>();
        }

        float progress = playerController != null ? playerController.DashCooldownProgress : 1f;
        dashCooldownSlider.SetValueWithoutNotify(progress);

        if (dashCooldownFill != null)
        {
            dashCooldownFill.color = Color.Lerp(dashCooldownEmptyColor, dashCooldownReadyColor, progress);
        }
    }

    private void RefreshTVRemoteCooldown()
    {
        if (tvRemoteCooldownSlider == null)
        {
            return;
        }

        if (tvRemoteAbility == null)
        {
            tvRemoteAbility = FindAnyObjectByType<TVRemoteAbility>();
        }

        float progress = GetTVRemoteCooldownProgress();
        tvRemoteCooldownSlider.SetValueWithoutNotify(progress);

        if (tvRemoteCooldownFill != null)
        {
            tvRemoteCooldownFill.color = Color.Lerp(tvRemoteCooldownEmptyColor, tvRemoteCooldownReadyColor, progress);
        }
    }

    private float GetTVRemoteCooldownProgress()
    {
        if (tvRemoteAbility == null)
        {
            return 1f;
        }

        float cooldownDuration = tvRemoteAbility.CooldownDuration;
        if (cooldownDuration <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(1f - (tvRemoteAbility.CooldownRemaining / cooldownDuration));
    }

    private void EnsureDashCooldownSlider()
    {
        if (dashCooldownSlider != null)
        {
            if (dashCooldownFill == null && dashCooldownSlider.fillRect != null)
            {
                dashCooldownFill = dashCooldownSlider.fillRect.GetComponent<Image>();
            }

            return;
        }

        dashCooldownSlider = CreateDashCooldownSlider();
    }

    private void EnsureTVRemoteCooldownSlider()
    {
        if (tvRemoteCooldownSlider != null)
        {
            if (tvRemoteCooldownFill == null && tvRemoteCooldownSlider.fillRect != null)
            {
                tvRemoteCooldownFill = tvRemoteCooldownSlider.fillRect.GetComponent<Image>();
            }

            return;
        }

        tvRemoteCooldownSlider = CreateTVRemoteCooldownSlider();
    }

    private Slider CreateDashCooldownSlider()
    {
        return CreateCooldownSlider("DashCooldownSlider", new Vector2(20f, -95f), dashCooldownReadyColor, out dashCooldownFill);
    }

    private Slider CreateTVRemoteCooldownSlider()
    {
        return CreateCooldownSlider("TVRemoteCooldownSlider", new Vector2(20f, -155f), tvRemoteCooldownReadyColor, out tvRemoteCooldownFill);
    }

    private Slider CreateCooldownSlider(string sliderName, Vector2 anchoredPosition, Color readyColor, out Image fillImage)
    {
        GameObject sliderObject = new GameObject(sliderName, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(transform, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = new Vector2(180f, 16f);

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(sliderObject.transform, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);

        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillAreaObject.transform, false);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillImage = fillObject.GetComponent<Image>();
        fillImage.color = readyColor;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return slider;
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
