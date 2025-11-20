using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Cube"))
        {
            Debug.Log("Projectile hit cube — KILL!");

            CubeTracker tracker = collision.gameObject.GetComponent<CubeTracker>();
            if (tracker != null)
            {
                // ✅ Mark as killed by projectile BEFORE calling RegisterKill
                // (in case RegisterKill triggers destruction or wave cleanup)
                tracker.killedByProjectile = true;

                if (tracker.waveManager != null)
                {
                    tracker.waveManager.RegisterKill();
                    Debug.Log("RegisterKill successfully called!");
                }
                else
                {
                    Debug.LogError("WaveManager reference missing on CubeTracker!"); 
                    Debug.LogError($"Cube name: {collision.gameObject.name}");
                    // ⚠️ Optional: still destroy cube, but don't count as kill
                }
            }
            else
            {
                Debug.LogError("CubeTracker component missing on cube!");
            }

            // ✅ Destroy cube and projectile regardless
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}