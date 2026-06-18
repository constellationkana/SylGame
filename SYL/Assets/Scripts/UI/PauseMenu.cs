using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Toggles a simple pause menu and freezes gameplay while paused.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    private static PauseMenu activeInstance;

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string pauseTitle = "Paused";

    private bool isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapPauseMenu()
    {
        if (SceneManager.GetActiveScene().name == MainMenuSceneName)
        {
            return;
        }

        if (FindAnyObjectByType<PauseMenu>() != null)
        {
            return;
        }

        new GameObject(nameof(PauseMenu)).AddComponent<PauseMenu>();
    }

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            enabled = false;
            return;
        }

        activeInstance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        EnsurePausePanel();
        ConfigurePausePanel();
        SetPausePanelVisible(false);
    }

    private void OnEnable()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(Quit);
        }
    }

    private void OnDisable()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(Quit);
        }
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }

        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        TogglePause();
    }

    /// <summary>
    /// Switches between paused and resumed gameplay.
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
            return;
        }

        Pause();
    }

    /// <summary>
    /// Pauses active gameplay, shows the pause panel, and unlocks the cursor.
    /// </summary>
    public void Pause()
    {
        if (!GameStateManager.Instance.IsPlaying)
        {
            return;
        }

        isPaused = true;
        GameStateManager.Instance.SetState(GameState.Paused);
        Time.timeScale = 0f;
        SetPausePanelVisible(true);
        UnlockCursor();

        if (resumeButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    /// <summary>
    /// Resumes paused gameplay, hides the pause panel, and restores the gameplay cursor lock.
    /// </summary>
    public void Resume()
    {
        if (!isPaused && !GameStateManager.Instance.IsPaused)
        {
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;

        if (GameStateManager.Instance.IsPaused)
        {
            GameStateManager.Instance.SetState(GameState.Playing);
        }

        SetPausePanelVisible(false);
        LockCursor();
    }

    /// <summary>
    /// Reserved for future main-menu scene flow from the pause menu.
    /// </summary>
    public void Quit()
    {
    }

    private void EnsurePausePanel()
    {
        if (pausePanel != null)
        {
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        EnsureEventSystem();
        pausePanel = CreatePausePanel(canvas.transform);
    }

    private void ConfigurePausePanel()
    {
        if (titleText != null)
        {
            titleText.text = pauseTitle;
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(Quit);
            quitButton.onClick.AddListener(Quit);
        }
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        EventSystem.current = eventSystemObject.GetComponent<EventSystem>();
    }

    private GameObject CreatePausePanel(Transform canvasTransform)
    {
        GameObject panel = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(320f, 220f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);

        titleText = CreateLabel(panel.transform, "Title", pauseTitle, 36f, new Vector2(0f, 60f), new Vector2(260f, 50f));
        resumeButton = CreateButton(panel.transform, "ResumeButton", "Resume", new Vector2(0f, 5f));
        quitButton = CreateButton(panel.transform, "QuitButton", "Quit", new Vector2(0f, -55f));

        return panel;
    }

    private TMP_Text CreateLabel(
        Transform parent,
        string objectName,
        string label,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TMP_Text text = labelObject.GetComponent<TMP_Text>();
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private Button CreateButton(Transform parent, string objectName, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(200f, 44f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text buttonText = CreateLabel(buttonObject.transform, "Label", label, 22f, Vector2.zero, new Vector2(180f, 34f));
        buttonText.color = Color.black;

        return button;
    }

    private void SetPausePanelVisible(bool isVisible)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isVisible);
        }
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
