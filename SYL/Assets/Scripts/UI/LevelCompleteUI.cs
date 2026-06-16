using TMPro;
using UnityEngine;

/// <summary>
/// Shows the level complete screen and stops gameplay after the player wins.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TMP_Text levelCompleteText;
    [SerializeField] private string levelCompleteMessage = "Level Complete";

    [Header("Disable On Complete")]
    [SerializeField] private Behaviour[] gameplayBehavioursToDisable = new Behaviour[0];

    private bool isLevelComplete;

    public bool IsLevelComplete
    {
        get { return isLevelComplete; }
    }

    private void Awake()
    {
        SetLevelCompletePanelVisible(false);
    }

    public void ShowLevelComplete()
    {
        if (isLevelComplete)
        {
            return;
        }

        isLevelComplete = true;

        if (levelCompleteText != null)
        {
            levelCompleteText.text = levelCompleteMessage;
        }

        SetLevelCompletePanelVisible(true);
        DisableGameplayBehaviours();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DisableGameplayBehaviours()
    {
        foreach (Behaviour gameplayBehaviour in gameplayBehavioursToDisable)
        {
            if (gameplayBehaviour != null && gameplayBehaviour != this)
            {
                gameplayBehaviour.enabled = false;
            }
        }
    }

    private void SetLevelCompletePanelVisible(bool isVisible)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(isVisible);
        }
    }
}
