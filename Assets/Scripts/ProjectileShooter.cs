using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float shootForce = 12f;
    public float spawnOffset = 0.25f; // meters in front of camera
    public float projectileLifetime = 8f;

    /// <summary>
    /// Called ONLY by SpellManager when a spell card is clicked.
    /// </summary>
    public void TryShoot(string spellName = "Unknown")
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
        if (proj == null)
        {
            Debug.LogError("[ProjectileShooter] Instantiate returned null!");
            return;
        }

        // Ensure Rigidbody
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = proj.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.linearVelocity = cam.forward * shootForce;

        // Ensure Collider
        if (proj.GetComponent<Collider>() == null)
        {
            SphereCollider sc = proj.AddComponent<SphereCollider>();
            sc.radius = 0.15f;
        }

        // Auto-destroy
        Destroy(proj, projectileLifetime);

        // 🔥 Debug: Map spell name to element
        string spellType = spellName switch
        {
            "Spell 1" => "Fire",
            "Spell 2" => "Water",
            "Spell 3" => "Thunder",
            _ => "Unknown"
        };

        Debug.Log($"[SPELL CAST] {spellName} → {spellType} projectile fired!");
    }
}