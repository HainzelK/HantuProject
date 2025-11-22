// PlayerHealth.cs
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("UI")]
    // 🔥 REMOVED prefab reference - now uses existing UI
    public Image fillImage; // Assign the "Fill" Image from your Canvas

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
        // 🔥 No CreateHpBar() needed - UI exists in scene
        UpdateHpBar();
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        UpdateHpBar();
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHpBar()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = currentHealth / maxHealth;
        }
    }

    void Die()
    {
        Debug.Log("PLAYER DIED!");
        // TODO: Show game over screen
    }
}