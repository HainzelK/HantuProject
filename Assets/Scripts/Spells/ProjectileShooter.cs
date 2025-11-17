using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float shootForce = 25f;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (IsTap())
            ShootProjectile();
    }

    private bool IsTap()
    {
        return 
            (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false) ||
            (Mouse.current?.leftButton.wasPressedThisFrame ?? false);
    }

    void ShootProjectile()
    {
        // spawn at camera
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * 0.1f;
        Quaternion rotation = Quaternion.LookRotation(cam.transform.forward);

        GameObject proj = Instantiate(projectilePrefab, spawnPos, rotation);

        // ensure projectile has Rigidbody
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = cam.transform.forward * shootForce;
        }

        Destroy(proj, 5f); // clean up later
    }
}
