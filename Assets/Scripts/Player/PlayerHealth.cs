// PlayerHealth.cs
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("UI")]
    public Image fillImage; // Assign the "Fill" Image from your Canvas

    [Header("Visual Feedback")]
    // Healing
    public bool flashOnHeal = true;
    public Color healFlashColor = new Color(0.7f, 1f, 0.7f, 1f);
    public float healFlashDuration = 0.3f; // ✅ WAS MISSING!

    // Damage
    public bool flashOnDamage = true;
    public Color damageFlashColor = new Color(1f, 0.7f, 0.7f, 1f);
    public float damageFlashDuration = 0.2f;

    private Coroutine activeFlashRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHpBar();
        Debug.Log($"[PlayerHealth] Initialized with HP: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0 || currentHealth <= 0) 
        {
            Debug.Log($"[PlayerHealth] ❌ Invalid damage: {amount} (HP: {currentHealth})");
            return;
        }

        float oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        UpdateHpBar();

        Debug.Log($"[PlayerHealth] 💔 Took {amount} damage! HP: {oldHealth} → {currentHealth}");

        if (flashOnDamage && fillImage != null)
        {
            StartFlash(damageFlashColor, damageFlashDuration);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0 || currentHealth >= maxHealth) 
        {
            Debug.Log($"[PlayerHealth] ❌ Invalid heal: {amount} (HP: {currentHealth}/{maxHealth})");
            return;
        }

        float oldHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHpBar();

        Debug.Log($"[PlayerHealth] 💚 Healed for {amount}! HP: {oldHealth} → {currentHealth}");

        if (flashOnHeal && fillImage != null)
        {
            StartFlash(healFlashColor, healFlashDuration); // ✅ Now works!
        }
    }

    void UpdateHpBar()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = currentHealth / maxHealth;
        }
    }

    void StartFlash(Color flashColor, float duration)
    {
        if (activeFlashRoutine != null)
        {
            StopCoroutine(activeFlashRoutine);
        }
        activeFlashRoutine = StartCoroutine(FlashRoutine(flashColor, duration));
    }

    System.Collections.IEnumerator FlashRoutine(Color flashColor, float duration)
    {
        Color originalColor = fillImage.color;
        fillImage.color = flashColor;
        yield return new WaitForSeconds(duration);
        fillImage.color = originalColor;
    }

    void Die()
    {
        Debug.Log("💀 PLAYER DIED!");
        // TODO: Show game over screen
    }
}