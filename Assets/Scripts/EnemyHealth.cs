using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    public GameObject enemyHpBarPrefab;
    private Image fillImage;
    private GameObject hpBarInstance; // Track bar instance

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[EnemyHealth] {name} initialized with HP: {currentHealth}");
        if (enemyHpBarPrefab != null)
        {
            CreateHpBar();
        }
        else
        {
            Debug.LogError($"[EnemyHealth] MISSING HP BAR PREFAB on {name}!");
        }
    }

void CreateHpBar()
{
    // 🔥 STEP 1: CREATE A SIMPLE RED CUBE (no prefab)
    GameObject debugBar = new GameObject("DEBUG_HP_BAR");
    debugBar.transform.localScale = Vector3.one * 0.2f; // Visible size
    
    // Add renderer
    MeshFilter mf = debugBar.AddComponent<MeshFilter>();
    mf.mesh = Mesh.Instantiate(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
    MeshRenderer mr = debugBar.AddComponent<MeshRenderer>();
    mr.material = new Material(Shader.Find("Standard"));
    mr.material.color = Color.red;

    // 🔥 STEP 2: PLACE IT DIRECTLY ABOVE ENEMY (WORLD SPACE)
    debugBar.transform.position = transform.position + Vector3.up * 2.0f;
    
    // 🔥 STEP 3: KEEP IT ALIVE
    DontDestroyOnLoad(debugBar);
    
    Debug.Log($"[DEBUG] Created RED CUBE at {debugBar.transform.position} for {name}");
}
    // Helper to list child names
    string[] GetChildNames(GameObject obj)
    {
        var names = new System.Collections.Generic.List<string>();
        foreach (Transform child in obj.transform)
        {
            names.Add(child.name);
        }
        return names.ToArray();
    }

    IEnumerator UpdatePosition()
    {
        Debug.Log($"[EnemyHealth] Starting HP bar position loop for {name}");
        int frameCount = 0;
        
        while (hpBarInstance != null && gameObject.activeInHierarchy)
        {
            if (Camera.main != null)
            {
                Vector3 worldPos = transform.position + Vector3.up * 1.5f;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                hpBarInstance.transform.position = screenPos;
                
                if (frameCount < 3) // Log first 3 frames
                {
                    Debug.Log($"[EnemyHealth] Frame {frameCount}: {name} HP bar at {screenPos} (world: {worldPos})");
                    frameCount++;
                }
            }
            else
            {
                Debug.LogWarning("[EnemyHealth] Camera.main is NULL!");
            }
            yield return null;
        }
        
        Debug.Log($"[EnemyHealth] HP bar loop ended for {name}");
        if (hpBarInstance != null) Destroy(hpBarInstance);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log($"[EnemyHealth] {name} took {amount} damage → HP: {currentHealth}");
        UpdateHpBar();
        if (currentHealth <= 0) Die();
    }

    void UpdateHpBar()
    {
        if (fillImage != null)
        {
            float fillAmount = currentHealth / maxHealth;
            fillImage.fillAmount = fillAmount;
            Debug.Log($"[EnemyHealth] {name} HP fill set to {fillAmount:P0}");
        }
    }

    void Die()
    {
        Debug.Log($"[EnemyHealth] {name} DIED!");
        if (hpBarInstance != null) Destroy(hpBarInstance);
        Destroy(gameObject);
    }
}