using UnityEngine;

public class ProjectileCollision : MonoBehaviour
{
    public float baseDamage = 50f; // Renamed from "damage"

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("MainCamera"))
        {
            PlayerHealth.Instance?.TakeDamage(baseDamage);
            Destroy(gameObject);
            return;
        }

        if (collision.gameObject.CompareTag("Cube"))
        {
            Debug.Log("Projectile hit cube!");

            // 🔥 GET CURRENT WAVE DAMAGE
            float currentDamage = baseDamage;
            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                currentDamage = baseDamage;
            }

            EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            CubeMover cubeMover = collision.gameObject.GetComponent<CubeMover>();

            cubeMover?.TakeDamageFromPlayer();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(currentDamage); // ← Use wave-based damage

                if (enemyHealth.CurrentHealth <= 0)
                {
                    cubeMover?.TriggerDeath();
                    HandleEnemyDeath(collision.gameObject);
                }
            }
            else
            {
                cubeMover?.TriggerDeath();
                HandleEnemyDeath(collision.gameObject);
            }

            Destroy(gameObject);
        }
    }

    void HandleEnemyDeath(GameObject enemy)
    {
        if (enemy == null) return;

        CubeTracker tracker = enemy.GetComponent<CubeTracker>();
        if (tracker != null)
        {
            tracker.killedByProjectile = true;

            if (tracker.waveManager != null)
            {
                if (tracker.waveManager.enemyIndicatorManager != null)
                {
                    tracker.waveManager.enemyIndicatorManager.UnregisterEnemy(enemy);
                }
                tracker.waveManager.RegisterKill();
                Debug.Log("Enemy killed by projectile! RegisterKill called.");
            }
            else
            {
                Debug.LogError("WaveManager reference missing on CubeTracker!");
                Debug.LogError($"Cube name: {enemy.name}");
                
                var indicatorMgr = FindObjectOfType<EnemyIndicatorManager>();
                if (indicatorMgr != null)
                {
                    indicatorMgr.UnregisterEnemy(enemy);
                }
            }
        }
        else
        {
            Debug.LogError("CubeTracker component missing on cube!");
            var indicatorMgr = FindObjectOfType<EnemyIndicatorManager>();
            if (indicatorMgr != null)
            {
                indicatorMgr.UnregisterEnemy(enemy);
            }
        }
    }
}