using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles the main menu buttons and keeps scene flow in one simple place.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Scene Flow")]
    [SerializeField] private string gameplaySceneName = "GameScene";

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private string gameTitle = "SylGame";

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (titleText != null)
        {
            titleText.text = gameTitle;
        }
    }

    private void OnEnable()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(Play);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(Quit);
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(Play);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(Quit);
        }
    }

    public void Play()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError($"{nameof(MainMenu)} needs a gameplay scene name before Play can load the game.", this);
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
