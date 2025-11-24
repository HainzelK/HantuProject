using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Spell Projectiles")]
    public GameObject thunderProjectile; // "Lette"
    public GameObject waterProjectile;   // "Uwai"
    public GameObject fireProjectile;    // "Spell 3"

    [Header("Projectile Settings")]
    public float shootForce = 12f;
    public float spawnOffset = 0.25f;
    public float projectileLifetime = 8f;

    public System.Action<string> onSpellCast;

    public void TryShoot(string spellName = "Unknown")
    {
        // 🔥 SKIP HEALING SPELLS — they don't use projectiles
        if (spellName == "sau")
        {
            Debug.LogWarning("[ProjectileShooter] 'Sau' is a healing spell — no projectile fired.");
            return;
        }

        // Normalize input to lowercase for robust matching
        string spellLower = spellName.ToLower();

        // 🔥 SELECT PREFAB BASED ON SPELL (case-insensitive)
        GameObject selectedPrefab = spellLower switch
        {
            "lette" or "spell 1" => thunderProjectile,
            "uwai" or "spell 2" => waterProjectile,
            "spell 3" or "api" => fireProjectile,
            _ => null // ← Just return null; handle error below
        };

        if (selectedPrefab == null)
        {
            Debug.LogError($"[ProjectileShooter] Unknown spell or missing prefab: '{spellName}'");
            return;
        }

        Transform cam = transform;
        Vector3 spawnPos = cam.TransformPoint(Vector3.forward * spawnOffset);
        Quaternion spawnRot = cam.rotation;

        GameObject proj = Instantiate(selectedPrefab, spawnPos, spawnRot);
        if (proj == null)
        {
            Debug.LogError("[ProjectileShooter] Instantiate returned null!");
            return;
        }

        SetProjectileColor(proj, spellLower);

        // Rigidbody
        Rigidbody rb = proj.GetComponent<Rigidbody>() ?? proj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = cam.forward * shootForce;

        // Collider
        if (proj.GetComponent<Collider>() == null)
        {
            proj.AddComponent<SphereCollider>().radius = 0.15f;
        }

        Destroy(proj, projectileLifetime);
        onSpellCast?.Invoke(spellName);

        // Debug log
        string spellType = spellLower switch
        {
            "lette" or "spell 1" => "Thunder",
            "uwai" or "spell 2" => "Water",
            "spell 3" or "api" => "Fire",
            _ => "Unknown"
        };
        Debug.Log($"[SPELL CAST] {spellName} → {spellType} projectile fired!");
    }

    void SetProjectileColor(GameObject projectile, string spellNameLower)
    {
        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer == null) return;

        renderer.material = new Material(renderer.material);
        
        Color spellColor = spellNameLower switch
        {
            "lette" or "spell 1" => Color.yellow,   // ⚡ Thunder
            "uwai" or "spell 2" => Color.blue,      // 💧 Water
            "spell 3" or "api" => Color.red,        // 🔥 Fire
            _ => Color.white
        };
        renderer.material.color = spellColor;
    }
}