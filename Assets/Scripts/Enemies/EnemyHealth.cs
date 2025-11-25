using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    // 🔥 Visual Feedback
    public Material hitMaterial; // Optional: assign custom hit material
    public Color hitFlashColor = Color.red; // Fallback if no hitMaterial
    public float hitFlashDuration = 0.25f;

    // 🔊 Audio Feedback
    public AudioClip hitSFX;
    public AudioClip deathSFX;
    public float sfxVolume = 1f;

    private Renderer enemyRenderer;
    private Material originalMaterial;

    void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[EnemyHealth] {name} initialized with HP: {currentHealth}");
        
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalMaterial = enemyRenderer.material;
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0) return;
        
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        Debug.Log($"[EnemyHealth] {name} took {amount} damage → HP: {currentHealth}");
        
        // 🔊 Play hit SFX
        if (hitSFX != null)
        {
            AudioSource.PlayClipAtPoint(hitSFX, transform.position, sfxVolume);
        }
        
        StartCoroutine(HitFlash());
        
        if (currentHealth <= 0) 
            Die();
    }

    IEnumerator HitFlash()
    {
        if (enemyRenderer == null || originalMaterial == null) 
            yield break;

        // 🔥 Use hitMaterial if assigned, otherwise use color flash
        Material flashMaterial = hitMaterial != null 
            ? new Material(hitMaterial) 
            : new Material(originalMaterial) { color = hitFlashColor };

        enemyRenderer.material = flashMaterial;
        yield return new WaitForSeconds(hitFlashDuration);

        if (enemyRenderer != null)
        {
            enemyRenderer.material = originalMaterial;
            Destroy(flashMaterial);
        }
    }

    void Die()
    {
        Debug.Log($"[EnemyHealth] {name} DIED!");

        // 🔊 Play death SFX
        if (deathSFX != null)
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position, sfxVolume);
        }

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