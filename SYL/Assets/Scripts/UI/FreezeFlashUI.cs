using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a short fullscreen white flash using a UI Image.
/// </summary>
public class FreezeFlashUI : MonoBehaviour
{
    private const string RuntimeFlashName = "FreezeFlashRuntime";

    [SerializeField] private Image flashImage;
    [Min(0.01f)]
    [SerializeField] private float flashDuration = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float flashAlpha = 0.85f;

    private static FreezeFlashUI authoritativeInstance;
    private static int lastFlashFrame = -1;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        ResolveFlashImage();
        ConfigureFlashImage();
        HideFlash();
        RegisterInstance();
    }

    private void OnEnable()
    {
        RegisterInstance();
    }

    private void OnDestroy()
    {
        if (authoritativeInstance == this)
        {
            authoritativeInstance = null;
        }
    }

    public static void PlayGlobalFlash()
    {
        if (lastFlashFrame == Time.frameCount)
        {
            return;
        }

        FreezeFlashUI instance = GetAuthoritativeInstance();
        if (instance == null)
        {
            return;
        }

        lastFlashFrame = Time.frameCount;
        instance.PlayFlashInternal();
    }

    public void PlayFlash()
    {
        if (this != GetAuthoritativeInstance())
        {
            PlayGlobalFlash();
            return;
        }

        if (lastFlashFrame == Time.frameCount)
        {
            return;
        }

        lastFlashFrame = Time.frameCount;
        PlayFlashInternal();
    }

    private void PlayFlashInternal()
    {
        ResolveFlashImage();
        ConfigureFlashImage();

        if (flashImage == null)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private static FreezeFlashUI GetAuthoritativeInstance()
    {
        if (IsRenderable(authoritativeInstance))
        {
            return authoritativeInstance;
        }

        FreezeFlashUI[] instances = FindObjectsByType<FreezeFlashUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        authoritativeInstance = SelectBestInstance(instances);

        if (authoritativeInstance != null)
        {
            authoritativeInstance.ResolveFlashImage();
            authoritativeInstance.ConfigureFlashImage();
            authoritativeInstance.HideFlash();
            return authoritativeInstance;
        }

        authoritativeInstance = CreateRuntimeInstance();
        return authoritativeInstance;
    }

    private static FreezeFlashUI SelectBestInstance(FreezeFlashUI[] instances)
    {
        foreach (FreezeFlashUI instance in instances)
        {
            if (instance == null)
            {
                continue;
            }

            if (IsRenderable(instance))
            {
                return instance;
            }
        }

        return null;
    }

    private static bool IsRenderable(FreezeFlashUI instance)
    {
        if (instance == null)
        {
            return false;
        }

        instance.ResolveFlashImage();
        return instance.flashImage != null
            && instance.flashImage.gameObject.activeInHierarchy
            && instance.flashImage.GetComponentInParent<Canvas>() != null;
    }

    private static FreezeFlashUI CreateRuntimeInstance()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("FreezeFlashCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject flashObject = new GameObject(RuntimeFlashName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(FreezeFlashUI));
        flashObject.transform.SetParent(canvas.transform, false);
        return flashObject.GetComponent<FreezeFlashUI>();
    }

    private void RegisterInstance()
    {
        if (authoritativeInstance == null || !IsRenderable(authoritativeInstance) || IsRenderable(this))
        {
            authoritativeInstance = this;
        }
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;
        SetFlashAlpha(flashAlpha);

        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fadeProgress = Mathf.Clamp01(elapsed / flashDuration);
            SetFlashAlpha(Mathf.Lerp(flashAlpha, 0f, fadeProgress));
            yield return null;
        }

        HideFlash();
        flashCoroutine = null;
    }

    private void SetFlashAlpha(float alpha)
    {
        Color color = Color.white;
        color.a = alpha;
        flashImage.gameObject.SetActive(true);
        flashImage.color = color;
        flashImage.enabled = alpha > 0f;
        flashImage.canvasRenderer.SetAlpha(alpha);
    }

    private void HideFlash()
    {
        if (flashImage != null)
        {
            SetFlashAlpha(0f);
        }
    }

    private void ResolveFlashImage()
    {
        if (flashImage == null)
        {
            flashImage = GetComponent<Image>();
        }
    }

    private void ConfigureFlashImage()
    {
        if (flashImage == null)
        {
            return;
        }

        EnsureUnderCanvas();
        flashImage.raycastTarget = false;
        flashImage.transform.SetAsLastSibling();

        RectTransform rectTransform = flashImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void EnsureUnderCanvas()
    {
        if (flashImage.GetComponentInParent<Canvas>() != null)
        {
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            flashImage.transform.SetParent(canvas.transform, false);
        }
    }

    private void OnValidate()
    {
        flashDuration = Mathf.Max(0.01f, flashDuration);
        flashAlpha = Mathf.Clamp01(flashAlpha);
    }
}
