// EnemyHealth.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f; // Set per wave in WaveManager
    public float currentHealth { get; private set; }
    public float CurrentHealth => currentHealth; // Public getter

    [Header("HP Bar")]
    public GameObject enemyHpBarPrefab;
    private Image fillImage;

    void Start()
    {
        currentHealth = maxHealth;
        if (enemyHpBarPrefab != null)
        {
            CreateHpBar();
        }
    }

    void CreateHpBar()
    {
        GameObject bar = Instantiate(enemyHpBarPrefab);
        bar.transform.localScale = Vector3.one;
        bar.transform.SetParent(null); // World space

        fillImage = bar.transform.Find("Fill")?.GetComponent<Image>();
        if (fillImage != null)
        {
            UpdateHpBar();
            StartCoroutine(UpdatePosition(bar));
        }
        else
        {
            Destroy(bar);
        }
    }

    IEnumerator UpdatePosition(GameObject bar)
    {
        while (bar != null && gameObject.activeInHierarchy)
        {
            if (Camera.main != null)
            {
                Vector3 worldPos = transform.position + Vector3.up * 1.5f;
                bar.transform.position = Camera.main.WorldToScreenPoint(worldPos);
            }
            yield return null;
        }
        if (bar != null) Destroy(bar);
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
        // Notify WaveManager via CubeTracker
        CubeTracker tracker = GetComponent<CubeTracker>();
        if (tracker?.waveManager != null && tracker.killedByProjectile)
        {
            tracker.waveManager.RegisterKill();
        }
        
        Destroy(gameObject);
    }
}