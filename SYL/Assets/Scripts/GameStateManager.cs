using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Provides a simple scene-local game state singleton for gameplay, pause, win, and game-over states.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    private static GameStateManager instance;

    [SerializeField] private GameState currentState = GameState.Playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ResetGameplayStateAfterSceneLoad()
    {
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name == MainMenuSceneName)
        {
            return;
        }

        Instance.ResetToPlaying();
    }

    /// <summary>
    /// Gets the active game state manager, creating one if the scene does not provide it.
    /// </summary>
    public static GameStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameStateManager>();

                if (instance == null)
                {
                    GameObject gameStateManagerObject = new GameObject(nameof(GameStateManager));
                    instance = gameStateManagerObject.AddComponent<GameStateManager>();
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// Gets the current high-level game state.
    /// </summary>
    public GameState CurrentState
    {
        get { return currentState; }
    }

    /// <summary>
    /// Gets whether gameplay is currently active.
    /// </summary>
    public bool IsPlaying
    {
        get { return currentState == GameState.Playing; }
    }

    /// <summary>
    /// Gets whether gameplay is currently paused.
    /// </summary>
    public bool IsPaused
    {
        get { return currentState == GameState.Paused; }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"Duplicate {nameof(GameStateManager)} found on {gameObject.name}. Disabling this instance.", this);
            enabled = false;
            return;
        }

        instance = this;
        ResetToPlaying();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// Updates the current high-level game state.
    /// </summary>
    /// <param name="state">The new state to apply.</param>
    public void SetState(GameState state)
    {
        currentState = state;
    }

    /// <summary>
    /// Restores gameplay state after scene loads or restarts.
    /// </summary>
    public void ResetToPlaying()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
    }
}
