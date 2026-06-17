using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager instance;

    [SerializeField] private GameState currentState = GameState.Playing;

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

    public GameState CurrentState
    {
        get { return currentState; }
    }

    public bool IsPlaying
    {
        get { return currentState == GameState.Playing; }
    }

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
        currentState = GameState.Playing;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void SetState(GameState state)
    {
        currentState = state;
    }
}
