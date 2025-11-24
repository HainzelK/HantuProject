using System.Collections;
using UnityEngine;


public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;

    public Material hitMaterial;
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    // 🔥 Use generic name (not "cube")
    private Renderer enemyRenderer;
    private Material originalMaterial;
    public float hitFlashDuration = 0.25f;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[EnemyHealth] {name} initialized with HP: {currentHealth}");
        
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            // 🔥 Cache the ORIGINAL material (do not modify it)
            originalMaterial = enemyRenderer.material;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log($"[EnemyHealth] {name} took {amount} damage → HP: {currentHealth}");
        
        StartCoroutine(HitFlash());
        
        if (currentHealth <= 0) 
            Die();
    }

IEnumerator HitFlash()
{
    Debug.Log($"[HitFlash] Started for {name}");
    
    if (enemyRenderer == null)
    {
        Debug.LogError($"[HitFlash] Renderer is NULL on {name}");
        yield break;
    }
    
    if (originalMaterial == null)
    {
        Debug.LogError($"[HitFlash] Original material is NULL on {name}");
        yield break;
    }

    Material flashMaterial = new Material(originalMaterial);
    flashMaterial.color = Color.red;
    enemyRenderer.material = flashMaterial;

    Debug.Log($"[HitFlash] Set to RED on {name}");

    yield return new WaitForSeconds(hitFlashDuration);

    if (enemyRenderer != null)
    {
        enemyRenderer.material = originalMaterial;
        Destroy(flashMaterial);
        Debug.Log($"[HitFlash] Restored original material on {name}");
    }
}

    void Die()
    {
        Debug.Log($"[EnemyHealth] {name} DIED! Notifying CubeMover for death animation...");

        CubeMover mover = GetComponent<CubeMover>();
        if (mover != null)
        {
            mover.TriggerDeath();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}