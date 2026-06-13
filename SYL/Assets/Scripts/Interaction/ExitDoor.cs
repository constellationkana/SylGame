using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private CollectibleObjective objective;

    private bool isUnlocked;

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
        if (!isUnlocked)
        {
            Debug.Log("Door is locked.", this);
            return;
        }

        Debug.Log("Level Complete.", this);
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
