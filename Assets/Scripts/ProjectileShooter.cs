using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float shootForce = 12f;
    public float spawnOffset = 0.25f; // meters in front of camera
    public float projectileLifetime = 8f;

    void Update()
    {
        // Support both touch (mobile) and mouse (editor)
        if (IsTap())
        {
            TryShoot();
        }
    }

    bool IsTap()
    {
        // Mobile: detect first touch began
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) return true;
        }

        // Editor / standalone: left mouse button down
        #if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) return true;
        #endif

        return false;
    }

    void TryShoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[ProjectileShooterEnhanced] projectilePrefab is NOT assigned!");
            return;
        }

        // Spawn position and rotation from camera
        Transform cam = transform;
        Vector3 spawnPos = cam.TransformPoint(Vector3.forward * spawnOffset);
        Quaternion spawnRot = cam.rotation;

        GameObject proj = Instantiate(projectilePrefab, spawnPos, spawnRot);
        if (proj == null)
        {
            Debug.LogError("[ProjectileShooterEnhanced] Instantiate returned null!");
            return;
        }

        // Ensure it has a Rigidbody
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = proj.AddComponent<Rigidbody>();
            rb.useGravity = false; // default to no gravity for AR projectile
        }

        // Ensure it has a Collider
        Collider col = proj.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = proj.AddComponent<SphereCollider>();
            sc.radius = 0.5f;
        }

        // Apply force
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(cam.forward * shootForce, ForceMode.Impulse);

        // Optional: tag for collision script (if you rely on tag checks)
        // proj.tag = "Projectile";

        // auto destroy
        Destroy(proj, projectileLifetime);

        Debug.Log("[ProjectileShooterEnhanced] Spawned projectile at " + spawnPos);
    }
}
