using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    // 🔥 HIT FEEDBACK FIELDS
    private Renderer cubeRenderer;
    private Color originalColor;
    public float hitFlashDuration = 0.25f; // ~15 frames at 60fps

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[EnemyHealth] {name} initialized with HP: {currentHealth}");
        
        // 🔥 CACHE ORIGINAL CUBE COLOR
        cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null && cubeRenderer.material != null)
        {
            originalColor = cubeRenderer.material.color;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log($"[EnemyHealth] {name} took {amount} damage → HP: {currentHealth}");
        
        // 🔥 TRIGGER HIT FEEDBACK
        StartCoroutine(HitFlash());
        
        if (currentHealth <= 0) Die();
    }

    // 🔥 HIT FEEDBACK COROUTINE
    IEnumerator HitFlash()
    {
        if (cubeRenderer == null) yield break;
        
        // Change to red
        cubeRenderer.material.color = Color.red;
        
        // Wait for duration
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Revert to original color
        if (cubeRenderer.material != null)
        {
            cubeRenderer.material.color = originalColor;
        }
    }

    void Die()
    {
        Debug.Log($"[EnemyHealth] {name} DIED!");
        Destroy(gameObject);
    }
}