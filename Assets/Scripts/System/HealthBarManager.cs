// HealthBarManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarManager : MonoBehaviour
{
    public static HealthBarManager Instance;

    [SerializeField] private GameObject hpBarPrefab; // Assign your HPBarEntry prefab

    private Dictionary<HealthSystem, GameObject> healthBars = new Dictionary<HealthSystem, GameObject>();
    private Camera mainCamera;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    public void RegisterHealthBar(HealthSystem healthSystem, Transform followTarget)
    {
        GameObject barObj = Instantiate(hpBarPrefab, transform);
        healthBars[healthSystem] = barObj;

        // Optional: Add a component to update position every frame
        HealthBarUI ui = barObj.AddComponent<HealthBarUI>();
        ui.Initialize(healthSystem, followTarget, mainCamera);
    }

    public void UnregisterHealthBar(HealthSystem healthSystem)
    {
        if (healthBars.TryGetValue(healthSystem, out GameObject barObj))
        {
            Destroy(barObj);
            healthBars.Remove(healthSystem);
        }
    }
}