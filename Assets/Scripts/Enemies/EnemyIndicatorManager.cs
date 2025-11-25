using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class EnemyIndicatorManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject indicatorPrefab;
    public Camera arCamera;

    public float indicatorDistanceFromEdge = 30f;

    [Header("Appearance")]
    // SAYA PERLEBAR: X (Lebar) jadi 200, Y (Tinggi) jadi 80
    public Vector2 indicatorSize = new Vector2(300f, 50f);

    [Header("Animation Settings")]
    [Range(0f, 1f)] public float maxOpacity = 0.8f;
    public float fadeInDuration = 0.5f;
    public float stayDuration = 0f;
    public float fadeOutDuration = 1.0f;

    private Dictionary<GameObject, GameObject> indicators = new Dictionary<GameObject, GameObject>();
    private Sprite arrowSprite;

    void Start()
    {
        if (arCamera == null) arCamera = Camera.main;

        arrowSprite = Resources.Load<Sprite>("UI/indicator");
        if (arrowSprite == null) Debug.LogError("Sprite 'Resources/UI/indicator' tidak ditemukan!");
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null || indicators.ContainsKey(enemy)) return;

        GameObject indicator = Instantiate(indicatorPrefab, transform);

        // Setup Image
        Image img = indicator.GetComponent<Image>();
        if (img != null)
        {
            if (arrowSprite != null) img.sprite = arrowSprite;
            Color c = img.color;
            c.a = 0f;
            img.color = c;
        }

        // Setup Ukuran & Pivot
        RectTransform rect = indicator.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = indicatorSize;
            // Penting: Pastikan pivot di tengah agar rotasi rapi
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        // Tambah Animasi
        IndicatorAnimator animator = indicator.AddComponent<IndicatorAnimator>();
        animator.Setup(maxOpacity, fadeInDuration, stayDuration, fadeOutDuration);

        indicator.SetActive(false);
        indicators[enemy] = indicator;
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (indicators.TryGetValue(enemy, out GameObject indicator))
        {
            Destroy(indicator);
            indicators.Remove(enemy);
        }
    }

    void LateUpdate()
    {
        foreach (var kvp in new Dictionary<GameObject, GameObject>(indicators))
        {
            if (kvp.Key == null)
            {
                UnregisterEnemy(kvp.Key);
                continue;
            }

            GameObject indicator = kvp.Value;
            if (indicator == null) continue;

            Vector3 screenPos = arCamera.WorldToScreenPoint(kvp.Key.transform.position);
            bool isBehind = Vector3.Dot(arCamera.transform.forward, kvp.Key.transform.position - arCamera.transform.position) < 0;
            bool isOnScreen = screenPos.z > 0 &&
                              screenPos.x >= 0 && screenPos.x <= Screen.width &&
                              screenPos.y >= 0 && screenPos.y <= Screen.height;

            if (!isOnScreen || isBehind)
            {
                if (!indicator.activeSelf) indicator.SetActive(true);

                // 1. Arah
                Vector3 viewportPos = arCamera.WorldToViewportPoint(kvp.Key.transform.position);
                if (isBehind)
                {
                    viewportPos.x = 1f - viewportPos.x;
                    viewportPos.y = 1f - viewportPos.y;
                }

                Vector2 dir = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);
                if (dir.magnitude < 0.01f) dir = Vector2.up;
                dir = dir.normalized;

                // 2. Rotasi
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                RectTransform rt = indicator.GetComponent<RectTransform>();
                rt.rotation = Quaternion.Euler(0, 0, angle - 90);

                // 3. POSISI (SAFE AREA LOGIC)
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                // MENGHITUNG DIAGONAL:
                // Karena panah bisa berputar, kita harus menggunakan panjang diagonal (Pythagoras)
                // sebagai batas aman, bukan hanya width/height. Ini agar sudut panah tidak keluar.
                float halfSize = Mathf.Sqrt((indicatorSize.x * indicatorSize.x) + (indicatorSize.y * indicatorSize.y)) / 2f;

                // Jarak Aman Total = Padding Custom + Setengah Diagonal Panah
                float safePadding = indicatorDistanceFromEdge + halfSize;

                Vector2 bounds = screenCenter - new Vector2(safePadding, safePadding);

                // Clamp posisi
                float divX = (dir.x != 0) ? bounds.x / Mathf.Abs(dir.x) : bounds.x;
                float divY = (dir.y != 0) ? bounds.y / Mathf.Abs(dir.y) : bounds.y;
                float scale = Mathf.Min(divX, divY);

                rt.position = screenCenter + (dir * scale);
            }
            else
            {
                indicator.SetActive(false);
            }
        }
    }
}

// ==========================================
// HELPER CLASS ANIMASI
// ==========================================
public class IndicatorAnimator : MonoBehaviour
{
    private Image targetImage;
    private float maxAlpha;
    private float fadeInTime;
    private float stayTime;
    private float fadeOutTime;
    private Coroutine animRoutine;

    public void Setup(float alpha, float fadeIn, float stay, float fadeOut)
    {
        targetImage = GetComponent<Image>();
        maxAlpha = alpha;
        fadeInTime = fadeIn;
        stayTime = stay;
        fadeOutTime = fadeOut;
    }

    private void OnEnable()
    {
        if (targetImage == null) targetImage = GetComponent<Image>();
        SetAlpha(0f);
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimateSequence());
    }

    private IEnumerator AnimateSequence()
    {
        // Fade In
        float timer = 0f;
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeInTime;
            SetAlpha(Mathf.Lerp(0f, maxAlpha, progress));
            yield return null;
        }
        SetAlpha(maxAlpha);

        // Stay
        yield return new WaitForSeconds(stayTime);

        // Fade Out
        timer = 0f;
        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeOutTime;
            SetAlpha(Mathf.Lerp(maxAlpha, 0f, progress));
            yield return null;
        }
        SetAlpha(0f);
    }

    private void SetAlpha(float a)
    {
        if (targetImage != null)
        {
            Color c = targetImage.color;
            c.a = a;
            targetImage.color = c;
        }
    }
}