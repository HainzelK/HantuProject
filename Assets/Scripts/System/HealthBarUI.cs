// HealthBarUI.cs
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    private HealthSystem targetHealth;
    private Transform followTarget;
    private Camera cam;
    private Image fillImage;

    void Start()
    {
        fillImage = GetComponentInChildren<Image>(); // Assumes "Fill" is a child with Image component
        // Or use: transform.Find("Fill").GetComponent<Image>();
    }

    public void Initialize(HealthSystem health, Transform target, Camera camera)
    {
        targetHealth = health;
        followTarget = target;
        cam = camera;
        UpdateFill();
    }

    void LateUpdate()
    {
        if (followTarget == null) return;

        // Convert world position to screen point
        Vector2 screenPos = cam.WorldToScreenPoint(followTarget.position + Vector3.up * 0.5f); // offset above object
        transform.position = screenPos;

        // Optionally hide if off-screen
        // if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
        //     gameObject.SetActive(false);
        // else
        //     gameObject.SetActive(true);
    }

    public void UpdateFill()
    {
        if (fillImage != null && targetHealth != null)
        {
            float pct = targetHealth.GetCurrentHealth() / targetHealth.GetMaxHealth();
            fillImage.fillAmount = pct;
        }
    }
}