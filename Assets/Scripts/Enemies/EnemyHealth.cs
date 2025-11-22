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
    Debug.Log($"[HP] STARTING CreateHpBar for {name}");
    
    if (enemyHpBarPrefab == null)
    {
        Debug.LogError($"[HP] ❌ HP BAR PREFAB IS NULL on {name}!");
        return;
    }

    hpBarInstance = Instantiate(enemyHpBarPrefab);
    
    if (hpBarInstance == null)
    {
        Debug.LogError("[HP] ❌ INSTANTIATION FAILED!");
        return;
    }

    // Log prefab structure
    Debug.Log($"[HP] ✅ Instantiated HP bar: {hpBarInstance.name}");
    Debug.Log($"[HP] Children in prefab: {string.Join(", ", GetChildNames(hpBarInstance))}");

    hpBarInstance.transform.SetParent(null);
    hpBarInstance.transform.localScale = Vector3.one;

    // Find Fill
    Transform fill = hpBarInstance.transform.Find("Fill");
    if (fill == null)
    {
        Debug.LogError("[HP] ❌ 'Fill' NOT FOUND! Check prefab child names.");
        Destroy(hpBarInstance);
        return;
    }

    fillImage = fill.GetComponent<Image>();
    if (fillImage == null)
    {
        Debug.LogError("[HP] ❌ 'Fill' has no Image component!");
        Destroy(hpBarInstance);
        return;
    }

    // Force visible color (override any material issues)
    fillImage.color = Color.green;
    Transform hpBg = hpBarInstance.transform.Find("HP");
    if (hpBg != null)
    {
        Image bgImage = hpBg.GetComponent<Image>();
        if (bgImage != null) bgImage.color = Color.red;
    }

    UpdateHpBar();
    StartCoroutine(UpdatePosition());
    Debug.Log($"[HP] ✅ HP bar fully created for {name}");
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