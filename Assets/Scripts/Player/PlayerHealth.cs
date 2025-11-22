// PlayerHealth.cs
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }
    
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("UI")]
    public GameObject playerHpBarPrefab; // Player-specific HP bar
    private Image fillImage;

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
        CreateHpBar();
    }

    void CreateHpBar()
    {
        if (playerHpBarPrefab == null) return;
        
        GameObject bar = Instantiate(playerHpBarPrefab);
        Canvas canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (canvas != null)
        {
            bar.transform.SetParent(canvas.transform, false);
            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -20);
        }

        fillImage = bar.transform.Find("Fill")?.GetComponent<Image>();
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