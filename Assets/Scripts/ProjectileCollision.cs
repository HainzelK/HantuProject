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
            // ✅ Mark as killed by projectile
            tracker.killedByProjectile = true;

            if (tracker.waveManager != null)
            {
                // ✅ Unregister from enemy indicators BEFORE destroying
                if (tracker.waveManager.enemyIndicatorManager != null)
                {
                    tracker.waveManager.enemyIndicatorManager.UnregisterEnemy(collision.gameObject);
                }

                tracker.waveManager.RegisterKill();
                Debug.Log("RegisterKill successfully called!");
            }
            else
            {
                Debug.LogError("WaveManager reference missing on CubeTracker!"); 
                Debug.LogError($"Cube name: {collision.gameObject.name}");
                
                // ✅ Still unregister if possible (fallback)
                var indicatorMgr = FindObjectOfType<EnemyIndicatorManager>();
                if (indicatorMgr != null)
                {
                    indicatorMgr.UnregisterEnemy(collision.gameObject);
                }
            }
        }
        else
        {
            Debug.LogError("CubeTracker component missing on cube!");
            
            // ✅ Fallback unregistration
            var indicatorMgr = FindObjectOfType<EnemyIndicatorManager>();
            if (indicatorMgr != null)
            {
                indicatorMgr.UnregisterEnemy(collision.gameObject);
            }
        }

        // ✅ Destroy cube and projectile
        Destroy(collision.gameObject);
        Destroy(gameObject);
    }
}
}