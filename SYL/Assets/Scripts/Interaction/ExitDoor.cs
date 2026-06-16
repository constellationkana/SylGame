using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private CollectibleObjective objective;
    [SerializeField] private LevelCompleteUI levelCompleteUI;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unlockClip;

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

        if (levelCompleteUI != null)
        {
            levelCompleteUI.ShowLevelComplete();
        }
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
