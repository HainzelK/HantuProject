using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;
    public float CurrentHealth => currentHealth;

    [Header("Visual Feedback")]
    public Color hitColor = Color.red;
    public float hitDuration = 0.25f;

    [Header("Audio")]
    public AudioClip hitSFX;
    public AudioClip deathSFX;
    public float sfxVolume = 1f;

    private MeshRenderer meshRenderer;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer != null && meshRenderer.material != null)
        {
            // Store original color (works for both URP and Built-in)
            originalColor = meshRenderer.material.color;
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0) return;
        
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        
        // 🔊 Play hit sound
        if (hitSFX != null)
            AudioSource.PlayClipAtPoint(hitSFX, transform.position, sfxVolume);
        
        // 🔴 Flash red
        StartCoroutine(HitFlash());
        
        if (currentHealth <= 0) 
            Die();
    }

    IEnumerator HitFlash()
    {
        if (meshRenderer == null) yield break;
        
        // 🔥 Direct color modification (URP-safe)
        Material mat = meshRenderer.material;
        Color original = mat.color;
        mat.color = hitColor; // Unity auto-maps to _BaseColor in URP
        
        yield return new WaitForSeconds(hitDuration);
        
        if (meshRenderer != null)
            meshRenderer.material.color = original;
    }

    void Die()
    {
        if (deathSFX != null)
            AudioSource.PlayClipAtPoint(deathSFX, transform.position, sfxVolume);

        var mover = GetComponent<CubeMover>();
        if (mover != null)
            mover.TriggerDeath();
        else
            Destroy(gameObject);
    }
}