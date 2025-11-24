using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    private Renderer cubeRenderer;
    private Color originalColor;
    public float hitFlashDuration = 0.25f;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[EnemyHealth] {name} initialized with HP: {currentHealth}");
        
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
        
        StartCoroutine(HitFlash());
        
        if (currentHealth <= 0) 
            Die();
    }

    IEnumerator HitFlash()
    {
        if (cubeRenderer == null) yield break;
        
        cubeRenderer.material.color = Color.red;
        yield return new WaitForSeconds(hitFlashDuration);
        
        if (cubeRenderer.material != null)
        {
            cubeRenderer.material.color = originalColor;
        }
    }

    void Die()
    {
        Debug.Log($"[EnemyHealth] {name} DIED! Notifying CubeMover for death animation...");

        // 🔥 DO NOT DESTROY HERE!
        // Let CubeMover handle animation and destruction.
        CubeMover mover = GetComponent<CubeMover>();
        if (mover != null)
        {
            mover.TriggerDeath(); // ✅ This plays "death" animation and destroys after delay
        }
        else
        {
            // Fallback: destroy immediately if no CubeMover (should not happen in normal setup)
            Destroy(gameObject);
        }
    }
}