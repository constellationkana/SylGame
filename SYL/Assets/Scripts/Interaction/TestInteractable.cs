using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log($"{nameof(TestInteractable)} was used on {gameObject.name}.", this);
    }
}
