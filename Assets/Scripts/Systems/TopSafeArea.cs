// TopSafeArea.cs
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TopSafeArea : MonoBehaviour
{
    void Start()
    {
        RectTransform rt = GetComponent<RectTransform>();
        Rect safeArea = Screen.safeArea;

        // Only adjust TOP inset
        float topInset = Screen.height - (safeArea.y + safeArea.height);
        float topRatio = topInset / Screen.height;

        // Preserve left/right/bottom, only move top down
        Vector2 anchorMin = rt.anchorMin;
        Vector2 anchorMax = rt.anchorMax;
        
        anchorMin.y = 1f - topRatio; // Push content down from top
        anchorMax.y = 1f;            // Keep top anchored to top

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
    }
}