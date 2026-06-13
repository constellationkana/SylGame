using UnityEngine;

[DisallowMultipleComponent]
public class FirstPersonHiddenVisual : MonoBehaviour
{
    [SerializeField] private Renderer[] renderersToHide;

    private bool[] originalRendererStates;

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnValidate()
    {
        if (renderersToHide == null || renderersToHide.Length == 0)
        {
            renderersToHide = GetComponentsInChildren<Renderer>(true);
        }
    }

    public void SetHiddenForFirstPerson(bool shouldHide)
    {
        CacheRenderers();

        for (int i = 0; i < renderersToHide.Length; i++)
        {
            Renderer rendererToHide = renderersToHide[i];
            if (rendererToHide == null)
            {
                continue;
            }

            rendererToHide.enabled = shouldHide ? false : originalRendererStates[i];
        }
    }

    private void CacheRenderers()
    {
        if (renderersToHide == null || renderersToHide.Length == 0)
        {
            renderersToHide = GetComponentsInChildren<Renderer>(true);
        }

        if (originalRendererStates != null && originalRendererStates.Length == renderersToHide.Length)
        {
            return;
        }

        originalRendererStates = new bool[renderersToHide.Length];
        for (int i = 0; i < renderersToHide.Length; i++)
        {
            originalRendererStates[i] = renderersToHide[i] != null && renderersToHide[i].enabled;
        }
    }
}
