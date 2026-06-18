using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Completes the level when interacted with after its assigned objective unlocks it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitDoor : MonoBehaviour, IInteractable
{
    [Header("Scene Flow")]
    [SerializeField] private string nextSceneName = "";
    [Min(0f)]
    [SerializeField] private float nextSceneLoadDelay = 0.75f;

    [SerializeField] private CollectibleObjective objective;
    [SerializeField] private LevelCompleteUI levelCompleteUI;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockClip;
    [SerializeField] private AudioClip levelCompleteClip;

    private bool isUnlocked;
    private bool levelCompleted;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Reset()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnEnable()
    {
        if (objective == null)
        {
            return;
        }

        objective.ObjectiveCompleted += HandleObjectiveCompleted;

        if (objective.IsComplete)
        {
            Unlock();
        }
    }

    private void OnDisable()
    {
        if (objective != null)
        {
            objective.ObjectiveCompleted -= HandleObjectiveCompleted;
        }
    }

    /// <summary>
    /// Attempts to complete the level if the exit has been unlocked.
    /// </summary>
    public void Interact()
    {
        if (levelCompleted)
        {
            return;
        }

        if (!isUnlocked)
        {
            return;
        }

        levelCompleted = true;
        PlaySound(levelCompleteClip);

        if (levelCompleteUI != null)
        {
            levelCompleteUI.ShowLevelComplete();
        }

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    private System.Collections.IEnumerator LoadNextSceneAfterDelay()
    {
        if (nextSceneLoadDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(nextSceneLoadDelay);
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"{nameof(ExitDoor)} cannot load scene '{nextSceneName}'. Add it to Build Settings or clear the next scene name.", this);
            yield break;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    private void HandleObjectiveCompleted()
    {
        Unlock();
    }

    private void Unlock()
    {
        if (isUnlocked)
        {
            return;
        }

        isUnlocked = true;
        PlaySound(unlockClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}
