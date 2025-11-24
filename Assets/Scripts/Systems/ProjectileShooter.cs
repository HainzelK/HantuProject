using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    [Header("Spell Projectiles")]
    public GameObject thunderProjectile; // Spell 1 / "Lette"
    public GameObject waterProjectile;   // Spell 2 / "Uwae"
    public GameObject fireProjectile;    // Spell 3

    [Header("Projectile Settings")]
    public float shootForce = 12f;
    public float spawnOffset = 0.25f;
    public float projectileLifetime = 8f;

    public System.Action<string> onSpellCast;

    public void TryShoot(string spellName = "Unknown")
    {
        // 🔥 SELECT PREFAB BASED ON SPELL
        GameObject selectedPrefab = spellName switch
        {
            "Spell 1" or "Lette" => thunderProjectile,
            "Spell 2" or "Uwae" => waterProjectile,
            "Spell 3" => fireProjectile,
            _ => thunderProjectile // fallback
        };

        if (selectedPrefab == null)
        {
            Debug.LogError($"[ProjectileShooter] Prefab missing for spell: {spellName}!");
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

        // 🔥 SET COLOR (optional - use if models need recoloring)
        SetProjectileColor(proj, spellName);

        // Rigidbody
        Rigidbody rb = proj.GetComponent<Rigidbody>() ?? proj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = cam.forward * shootForce;

        // Collider (only add if missing)
        if (proj.GetComponent<Collider>() == null)
        {
            // Use model's bounds or default size
            proj.AddComponent<SphereCollider>().radius = 0.15f;
        }

        Destroy(proj, projectileLifetime);
        onSpellCast?.Invoke(spellName);

        // Debug log
        string spellType = spellName switch
        {
            "Lette" => "Thunder",
            "Uwae" => "Water",
            "Spell 3" => "Fire",
            _ => "Unknown"
        };
        Debug.Log($"[SPELL CAST] {spellName} → {spellType} projectile fired!");
    }

    void SetProjectileColor(GameObject projectile, string spellName)
    {
        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer == null) return;

        // Only recolor if you want to override model's material
        renderer.material = new Material(renderer.material);
        
        Color spellColor = spellName switch
        {
            "Spell 1" or "lette" => Color.yellow,           // ⚡ Thunder
            "Spell 2" or "uwai" => Color.blue,          // 💧 Water
            "Spell 3" or "api" => Color.red,        // 🔥 Fire
            _ => Color.white
        };
        renderer.material.color = spellColor;
    }
}