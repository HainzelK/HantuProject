using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EdgeFlash : MonoBehaviour
{
    [Tooltip("Image UI yang menutupi layar untuk efek flash warna")]
    public Image overlayImage;

    [Tooltip("Durasi fade in/out dalam detik")]
    public float fadeDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private bool isFading;

    void Awake()
    {
        if (overlayImage == null)
        {
            Debug.LogError("EdgeFlash: overlayImage belum di-assign!");
            enabled = false;
            return;
        }

        canvasGroup = overlayImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = overlayImage.gameObject.AddComponent<CanvasGroup>();

        // Mulai dalam keadaan transparan
        canvasGroup.alpha = 0f;
        overlayImage.color = Color.white;
    }

    /// <summary>
    /// Memulai efek flash warna di layar (misal: merah untuk damage, hijau untuk healing)
    /// </summary>
    /// <param name="color">Warna efek</param>
    /// <param name="intensity">Kecerahan (0–1)</param>
    public void Trigger(Color color, float intensity = 0.3f)
    {
        if (isFading) return;
        StartCoroutine(FadeRoutine(color, intensity));
    }

    IEnumerator FadeRoutine(Color color, float intensity)
    {
        isFading = true;
        overlayImage.color = color;
        float targetAlpha = Mathf.Clamp01(intensity);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, targetAlpha, elapsed / fadeDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;

        // Tahan sebentar
        yield return new WaitForSecondsRealtime(0.1f);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(targetAlpha, 0f, elapsed / fadeDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;

        isFading = false;
    }
}