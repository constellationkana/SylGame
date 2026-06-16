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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip levelCompleteClip;

    [Header("Disable On Complete")]
    [SerializeField] private Behaviour[] gameplayBehavioursToDisable = new Behaviour[0];

    private bool isLevelComplete;

    public bool IsLevelComplete
    {
        get { return isLevelComplete; }
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        SetLevelCompletePanelVisible(false);
    }

    private void Reset()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void ShowLevelComplete()
    {
        if (isLevelComplete)
        {
            return;
        }

        isLevelComplete = true;
        PlaySound(levelCompleteClip);

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

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}
