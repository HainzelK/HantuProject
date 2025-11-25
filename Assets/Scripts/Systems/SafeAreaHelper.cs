// SafeAreaHelper.cs
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaHelper : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect();
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // Only update if safe area or orientation changed
        if (lastSafeArea != Screen.safeArea || lastOrientation != Screen.orientation)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        lastSafeArea = Screen.safeArea;
        lastOrientation = Screen.orientation;

        // Convert safe area from screen space to canvas space
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position / Screen.width;
        Vector2 anchorMax = (safeArea.position + safeArea.size) / Screen.width;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}