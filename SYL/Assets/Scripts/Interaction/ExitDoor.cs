using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private CollectibleObjective objective;
    [SerializeField] private LevelCompleteUI levelCompleteUI;

    private bool isUnlocked;
    private bool levelCompleted;

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
            Debug.Log("Door is locked.", this);
            return;
        }

        levelCompleted = true;
        Debug.Log("Level Complete.", this);

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
        Debug.Log("Exit door unlocked.", this);
    }
}
