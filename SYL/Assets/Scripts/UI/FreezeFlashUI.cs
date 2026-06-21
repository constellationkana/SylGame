using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays a short fullscreen white flash using a UI Image.
/// </summary>
public class FreezeFlashUI : MonoBehaviour
{
    [SerializeField] private Image flashImage;
    [Min(0.01f)]
    [SerializeField] private float flashDuration = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float flashAlpha = 0.85f;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        ResolveFlashImage();
        ConfigureFlashImage();
        HideFlash();
    }

    public void PlayFlash()
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
