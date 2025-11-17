using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float maxDistance = 50f; // how far your hitscan bullet can hit

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (IsTap())
            ShootHitscan();
    }

    private bool IsTap()
    {
        return 
            (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false) ||
            (Mouse.current?.leftButton.wasPressedThisFrame ?? false);
    }

    void ShootHitscan()
    {
        // ===== Hitscan Raycast =====
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            Debug.Log("Hit: " + hit.collider.name);
            // TODO: call hit effects / damage script here
        }

        // ===== OPTIONAL: spawn tiny bullet visual =====
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * 0.1f;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // make it very small
        proj.transform.localScale = Vector3.one * 0.2f;

        // move forward visually only (no physics)
        proj.AddComponent<VisualBullet>().Init(cam.transform.forward);

        // delete after 0.2 sec
        Destroy(proj, 0.2f);
    }
}
