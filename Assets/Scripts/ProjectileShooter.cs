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

    // 🔥 SET COLOR BASED ON SPELL
    SetProjectileColor(proj, spellName);

    // Rigidbody
    Rigidbody rb = proj.GetComponent<Rigidbody>() ?? proj.AddComponent<Rigidbody>();
    rb.useGravity = false;
    rb.linearVelocity = cam.forward * shootForce;

    // Collider
    if (proj.GetComponent<Collider>() == null)
    {
        proj.AddComponent<SphereCollider>().radius = 0.15f;
    }

    Destroy(proj, projectileLifetime);

    // Debug log
    string spellType = spellName switch
    {
        "Spell 1" => "Fire",
        "Spell 2" => "Water",
        "Spell 3" => "Thunder",
        _ => "Unknown"
    };
    Debug.Log($"[SPELL CAST] {spellName} → {spellType} projectile fired!");
}

void SetProjectileColor(GameObject projectile, string spellName)
{
    // Get the Renderer (MeshRenderer or SkinnedMeshRenderer)
    Renderer renderer = projectile.GetComponent<Renderer>();
    if (renderer == null)
    {
        Debug.LogWarning("Projectile has no Renderer — can't set color!");
        return;
    }

    // Ensure the material is unique (avoid changing prefab material)
    renderer.material = new Material(renderer.material);

    // Set color based on spell
    Color spellColor = spellName switch
    {
        "Spell 1" => Color.red,           // 🔥 Fire
        "Spell 2" => Color.blue,          // 💧 Water
        "Spell 3" => Color.yellow,        // ⚡ Thunder
        _ => Color.white
    };

    renderer.material.color = spellColor;
}

}