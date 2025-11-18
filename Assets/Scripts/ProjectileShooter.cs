using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float shootForce = 12f;
    public float spawnOffset = 0.25f;
    public float projectileLifetime = 8f;

    [Header("Cooldown")]
    public float shootCooldown = 0.2f; // seconds between shots
    private float lastShootTime = 0f;

    void Update()
    {
        bool tapDetected = false;

        // Touch detection
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            tapDetected = true;

        // Mouse detection (editor / standalone)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            tapDetected = true;

        // Shoot if tap detected and cooldown elapsed
        if (tapDetected && Time.time - lastShootTime >= shootCooldown)
        {
            ShootProjectile();
            lastShootTime = Time.time;
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[ProjectileShooter] projectilePrefab is NOT assigned!");
            return;
        }

        Transform cam = transform;
        Vector3 spawnPos = cam.TransformPoint(Vector3.forward * spawnOffset);
        Quaternion spawnRot = cam.rotation;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);

        // Rigidbody
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = proj.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }

        // Collider
        Collider col = proj.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = proj.AddComponent<SphereCollider>();
            sc.radius = 0.5f;
        }

        // Apply force
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(cam.forward * shootForce, ForceMode.Impulse);

        // Auto destroy
        Destroy(proj, projectileLifetime);

        Debug.Log("[ProjectileShooter] Spawned projectile at " + spawnPos);
    }
}
