using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Freezes an enemy without disabling its behavior scripts or resetting their state machines.
/// </summary>
[DisallowMultipleComponent]
public class EnemyTimePauseController : MonoBehaviour, ITimePausable
{
    [Header("References")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("Flash")]
    [SerializeField] private Renderer flashRenderer;
    [Min(0f)]
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private Color flashColor = Color.white;

    [Header("Events")]
    [SerializeField] private UnityEvent paused = new UnityEvent();
    [SerializeField] private UnityEvent resumed = new UnityEvent();

    private const string BaseColorPropertyName = "_BaseColor";
    private const string ColorPropertyName = "_Color";

    private int pauseCount;
    private bool agentWasStopped;
    private Vector3 agentVelocity;
    private Material[] flashMaterials = new Material[0];
    private Color[] originalBaseColors = new Color[0];
    private Color[] originalColors = new Color[0];
    private Coroutine flashCoroutine;

    public bool IsTimePaused
    {
        get { return pauseCount > 0; }
    }

    public float LastPauseStartedAt { get; private set; }

    public UnityEvent Paused
    {
        get { return paused; }
    }

    public UnityEvent Resumed
    {
        get { return resumed; }
    }

    private void Awake()
    {
        ResolveReferences();
        CacheFlashColors();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    public void PauseTime()
    {
        pauseCount++;

        if (pauseCount > 1)
        {
            return;
        }

        LastPauseStartedAt = Time.time;
        PauseAgent();
        PlayFlash();
        paused?.Invoke();
    }

    public void ResumeTime()
    {
        if (pauseCount == 0)
        {
            return;
        }

        pauseCount--;

        if (pauseCount > 0)
        {
            return;
        }

        ResumeAgent();
        resumed?.Invoke();
    }

    private void PauseAgent()
    {
        if (!CanUseAgent())
        {
            return;
        }

        agentWasStopped = navMeshAgent.isStopped;
        agentVelocity = navMeshAgent.velocity;
        navMeshAgent.isStopped = true;
        navMeshAgent.velocity = Vector3.zero;
    }

    private void ResumeAgent()
    {
        if (!CanUseAgent())
        {
            return;
        }

        navMeshAgent.isStopped = agentWasStopped;

        if (!agentWasStopped)
        {
            navMeshAgent.velocity = agentVelocity;
        }
    }

    private bool CanUseAgent()
    {
        return navMeshAgent != null
            && navMeshAgent.enabled
            && navMeshAgent.isOnNavMesh;
    }

    private void ResolveReferences()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
    }

    private void CacheFlashColors()
    {
        Renderer[] renderers = GetFlashRenderers();
        List<Material> materials = new List<Material>();

        foreach (Renderer rendererToFlash in renderers)
        {
            if (rendererToFlash != null)
            {
                materials.AddRange(rendererToFlash.materials);
            }
        }

        flashMaterials = materials.ToArray();
        originalBaseColors = new Color[flashMaterials.Length];
        originalColors = new Color[flashMaterials.Length];

        for (int i = 0; i < flashMaterials.Length; i++)
        {
            Material material = flashMaterials[i];

            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(BaseColorPropertyName))
            {
                originalBaseColors[i] = material.GetColor(BaseColorPropertyName);
            }

            if (material.HasProperty(ColorPropertyName))
            {
                originalColors[i] = material.GetColor(ColorPropertyName);
            }
        }
    }

    private void PlayFlash()
    {
        if (flashRenderer == null && GetComponentsInChildren<Renderer>().Length == 0)
        {
            return;
        }

        if (flashMaterials == null || flashMaterials.Length == 0)
        {
            CacheFlashColors();
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            RestoreFlashColors();
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetFlashColor(flashColor);

        if (flashDuration > 0f)
        {
            yield return new WaitForSeconds(flashDuration);
        }

        RestoreFlashColors();
        flashCoroutine = null;
    }

    private void SetFlashColor(Color color)
    {
        foreach (Material material in flashMaterials)
        {
            if (material == null)
            {
                continue;
            }

            if (material.HasProperty(BaseColorPropertyName))
            {
                material.SetColor(BaseColorPropertyName, color);
            }

            if (material.HasProperty(ColorPropertyName))
            {
                material.SetColor(ColorPropertyName, color);
            }
        }
    }

    private void RestoreFlashColors()
    {
        if (flashMaterials == null || originalBaseColors == null || originalColors == null)
        {
            return;
        }

        for (int i = 0; i < flashMaterials.Length; i++)
        {
            Material material = flashMaterials[i];

            if (material == null)
            {
                continue;
            }

            if (i < originalBaseColors.Length && material.HasProperty(BaseColorPropertyName))
            {
                material.SetColor(BaseColorPropertyName, originalBaseColors[i]);
            }

            if (i < originalColors.Length && material.HasProperty(ColorPropertyName))
            {
                material.SetColor(ColorPropertyName, originalColors[i]);
            }
        }
    }

    private Renderer[] GetFlashRenderers()
    {
        if (flashRenderer != null)
        {
            return new[] { flashRenderer };
        }

        return GetComponentsInChildren<Renderer>();
    }

    private void OnValidate()
    {
        flashDuration = Mathf.Max(0f, flashDuration);
    }
}
