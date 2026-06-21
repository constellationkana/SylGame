using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Player ability that pauses nearby enemies for a short duration.
/// </summary>
public class TVRemoteAbility : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference activateAction;
    [SerializeField] private Key keyboardFallbackKey = Key.Q;

    [Header("Freeze")]
    [Min(0f)]
    [SerializeField] private float cooldownDuration = 6f;
    [Min(0f)]
    [SerializeField] private float freezeDuration = 2.5f;
    [Min(0f)]
    [SerializeField] private float range = 8f;
    [SerializeField] private LayerMask enemyLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip freezeSuccessSound;
    [SerializeField] private AudioClip freezeFailureSound;

    [Header("Events")]
    [SerializeField] private UnityEvent freezeSucceeded = new UnityEvent();
    [SerializeField] private UnityEvent freezeFailed = new UnityEvent();

    private readonly Collider[] overlapResults = new Collider[64];
    private float nextReadyTime;

    public event Action<ITimePausable> EnemyFrozen;

    public float CooldownDuration
    {
        get { return cooldownDuration; }
    }

    public float FreezeDuration
    {
        get { return freezeDuration; }
    }

    public float Range
    {
        get { return range; }
    }

    public bool IsReady
    {
        get { return Time.time >= nextReadyTime; }
    }

    public float CooldownRemaining
    {
        get { return Mathf.Max(0f, nextReadyTime - Time.time); }
    }

    private void OnEnable()
    {
        ResolveAudioSource();
        activateAction?.action?.Enable();
    }

    private void OnDisable()
    {
        activateAction?.action?.Disable();
    }

    private void Update()
    {
        if (GameStateManager.Instance.IsPaused)
        {
            return;
        }

        if (WasActivatePressed())
        {
            TryActivate();
        }
    }

    public bool TryActivate()
    {
        if (!IsReady)
        {
            return false;
        }

        HashSet<ITimePausable> targets = FindTargetsInRange();

        if (targets.Count == 0)
        {
            PlaySound(freezeFailureSound);
            freezeFailed?.Invoke();
            return false;
        }

        nextReadyTime = Time.time + cooldownDuration;

        foreach (ITimePausable target in targets)
        {
            StartCoroutine(FreezeTarget(target));
            EnemyFrozen?.Invoke(target);
        }

        PlaySound(freezeSuccessSound);
        FreezeFlashUI.PlayGlobalFlash();
        freezeSucceeded?.Invoke();
        return true;
    }

    private bool WasActivatePressed()
    {
        if (activateAction != null && activateAction.action != null)
        {
            return activateAction.action.WasPressedThisFrame();
        }

        return Keyboard.current != null
            && Keyboard.current[keyboardFallbackKey].wasPressedThisFrame;
    }

    private HashSet<ITimePausable> FindTargetsInRange()
    {
        HashSet<ITimePausable> targets = new HashSet<ITimePausable>();
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            range,
            overlapResults,
            enemyLayers,
            triggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];
            if (hit == null)
            {
                continue;
            }

            AddPausables(hit.GetComponentsInParent<MonoBehaviour>(), targets);
            AddPausables(hit.GetComponentsInChildren<MonoBehaviour>(), targets);
        }

        return targets;
    }

    private void AddPausables(MonoBehaviour[] behaviours, HashSet<ITimePausable> targets)
    {
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ITimePausable pausable)
            {
                targets.Add(pausable);
            }
        }
    }

    private IEnumerator FreezeTarget(ITimePausable target)
    {
        target.PauseTime();
        yield return new WaitForSeconds(freezeDuration);
        target.ResumeTime();
    }

    private void PlaySound(AudioClip clip)
    {
        ResolveAudioSource();

        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void ResolveAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnValidate()
    {
        cooldownDuration = Mathf.Max(0f, cooldownDuration);
        freezeDuration = Mathf.Max(0f, freezeDuration);
        range = Mathf.Max(0f, range);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
