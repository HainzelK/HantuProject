using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube"))
        {
            CubeTracker tracker = collision.gameObject.GetComponent<CubeTracker>();
            if (tracker != null)
            {
                tracker.MarkKilledByProjectile();
            }
            else
            {
                Debug.LogWarning("Cube has no CubeTracker! Destroying fallback.");
                Destroy(collision.gameObject);
            }

            Destroy(gameObject); // Destroy the projectile
        }
    }
}
