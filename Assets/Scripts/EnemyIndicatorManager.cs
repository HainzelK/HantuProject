// EnemyIndicatorManager.cs
using UnityEngine;
using System.Collections.Generic;

public class EnemyIndicatorManager : MonoBehaviour
{
    public GameObject indicatorPrefab; // UI Image prefab
    public Camera arCamera;
    public float indicatorDistanceFromEdge = 30f; // pixels from screen edge

    private Dictionary<GameObject, GameObject> indicators = new Dictionary<GameObject, GameObject>();

    void Start()
    {
        if (arCamera == null) arCamera = Camera.main;
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null || indicators.ContainsKey(enemy)) return;
        
        GameObject indicator = Instantiate(indicatorPrefab, transform);
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

            // Convert enemy position to screen point
            Vector3 screenPos = arCamera.WorldToScreenPoint(kvp.Key.transform.position);
            
            // Check if enemy is behind camera
            bool isBehind = Vector3.Dot(arCamera.transform.forward, kvp.Key.transform.position - arCamera.transform.position) < 0;
            
            // Check if on screen
            bool isOnScreen = screenPos.z > 0 && 
                              screenPos.x >= 0 && screenPos.x <= Screen.width &&
                              screenPos.y >= 0 && screenPos.y <= Screen.height;

            if (!isOnScreen || isBehind)
            {
                indicator.SetActive(true);
                
                // Calculate direction from center to enemy
                Vector3 viewportPos = arCamera.WorldToViewportPoint(kvp.Key.transform.position);
                Vector2 dir = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);
                
                if (dir.magnitude < 0.01f) dir = Vector2.up;
                dir = dir.normalized;
                
                // Position on screen edge
                Vector2 edgePos = new Vector2(
                    0.5f + dir.x * 0.5f,
                    0.5f + dir.y * 0.5f
                );
                
                // Convert to screen coordinates
                Vector2 screenEdge = new Vector2(
                    edgePos.x * Screen.width,
                    edgePos.y * Screen.height
                );
                
                // Apply to indicator (using anchored position)
                RectTransform rect = indicator.GetComponent<RectTransform>();
                rect.position = screenEdge;
            }
            else
            {
                indicator.SetActive(false);
            }
        }
    }
}